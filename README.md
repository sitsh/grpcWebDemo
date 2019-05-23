# grpc-web Envoy .net core3 grpcServer 

## 记录 前端 js 通过 Envoy 反代服务器 调用 .net core3 grpcServer 实验过程

![整体流程图](https://user-gold-cdn.xitu.io/2019/1/28/16892894a56a9773?imageView2/0/w/1280/h/960/format/webp/ignore-error/1)


## 环境
* nodejs npm 主要前端编译库的需要
* .net core 3 sdk pv3
* vs2019 pv
* docker ubuntu18.04
* caddyServer 1.0
* chrome74


## 实验预期效果 
* grpc-web clinet 一次请求 一次响应调用
* grpc-web clinet ServerSteam 一次请求 Server 流式输出响应

## 端点配置 部署环境


| 端点       | url            | 描述       |
|----------|----------------|----------|
| grpc-web | localhost:8081 | web 前端页面 |
| EnvoyServer    |loclhost:8080|反代-docker 部署           |
| grpcServer netcore3|loclhost:9090| rpc接口 docker 部署         |



### grpcServer 端点构建

通过 vs2019 grpc模板  创建一个grpcServer项目
这个模板  已经自带了个简单 helloworld rpc方法了 我们这里 简单修改下 .proto 缓冲层定义 加入ServerSteam rpc 方法定义

```csharp


syntax = "proto3";

package helloworld;

service Greeter {
  // unary call
  rpc SayHello(HelloRequest) returns (HelloReply);
  // server streaming call
  rpc SayRepeatHello(HelloRequest) returns (stream HelloReply);
  // unary call - response after a length delay
  rpc SayHelloAfterDelay(HelloRequest) returns (HelloReply);
}

message HelloRequest {
  string name = 1;
}

message RepeatHelloRequest {
  string name = 1;
  int32 count = 2;
}

message HelloReply {
  string message = 1;
}
```

2个rpc 端点实现代码
```csharp
public class GreeterService : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = " grpcServer9090say: Hello  " + request.Name
            });
        }
        public override async Task SayRepeatHello(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {

            for (int i = 0; i <= 10; i++)
            {
                await responseStream.WriteAsync(new HelloReply { Message = " grpcServer9090say: Hello  "+i+"--" + request.Name });

                await Task.Delay(1000);
            }

            //return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }
    }
```

因为是简单演示 没有上 tls 证书 和 https
项目最后会以 docker形式 发布 所以这里我们修改了下默认 Kestrel 端口 改为了 <u>**9090**</u>

vs 中添加项目的 docker 支持 选 linux 平台后 vs 会帮你生成 项目部署的 dockerfile
ps: 我这里vs默认生成 dockerfile build 的时候报错 提示路径不对。没有深入研究 简单修改 dockerfile 里面的 路径 后 就可以编译了

```bash
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 9090

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src/grpcWebDemo
COPY ["grpcWebDemo.csproj", "/src/grpcNetCoreServer"]
RUN dotnet restore "/src/grpcNetCoreServer/grpcWebDemo.csproj"
COPY . .
WORKDIR "/src/grpcNetCoreServer"
RUN dotnet build "grpcWebDemo.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "grpcWebDemo.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "grpcWebDemo.dll"]
```

可以用命令 试着编译下一下 刚刚生成的 dockerfile 如果没有报错 就可以了先放着 等会会用 
docker-compose 把 Envoy 和 netcore grpc 部署在一个 docker 网段内

> docker build -t helloworld/grpcnetcoredemo -f Dockerfile .

### Envoy 部署
因为 Envoy 官方没有提供 二进制的 安装包 我们通过 grpc-web 官网的部署指引 把Envoy部署在 docker中

其中  envoy.yaml 是 Envoy Server 的配置文件 这里我们需要 调整一下的是 **上游**(netcore grpc api) 服务器地址这行的配置 需要按你自己的环境来修改-下面的这行配置按你 环境的区别 填写

>    hosts: [{ socket_address: { address: netcore-server, port_value: 9090 }}]

我们这里使用 ‘netcore-server’ 下面要说到的 docker-compose 中的 容器名称来替代实际地址

docker-compose.yml 配置如下

```bash
version: '3'
services:
  
  netcore-server:
    build:
      context: ./
      dockerfile: ./Dockerfile
    image: helloworld/grpcnetcoredemo:0.0.1
    ports:
      - "9090:9090"
  envoy:
    build:
      context: ./
      dockerfile: ./envoy/Dockerfile
    depends_on:
      - netcore-server
    image: helloworld/envoy
    ports:
      - "8080:8080"
    links:
      - netcore-server
```

如果上面的 配置 都没错通过 执行
> docker-compose up -d netcore-server envoy 

就能把 2 个 容器 拉起来了。 端口 分别是 8080 和 9090 2个docker 容器 
到这里 服务端的 envoy 反代 和 grpc netcore-server  已经分别部署好了


**在进行 前端grpc-web 之前 建议先测试下 2个容器**

envoy 反代 就需要关注 上游端点 ip  地址配置这一行。
grpc netcore-server 我们拉起 9090 这个容器后 可以用c#的 grpc Clinet 代码直接调用下刚才写的 2个grpc 接口
看看是否成功。


## grpc-web

grpcWebClient 前端项目文件夹


### .proto 自动生成 js文件
首先需要把 netcore-server 端点中的 .proto 缓冲层协议文件 通过工具生成 xx-pb.js 文件

* protoc [release](https://github.com/protocolbuffers/protobuf/releases)
* protoc-gen-grpc-web [protoc plugin] [releases](https://github.com/grpc/grpc-web/releases)

分别解压 上面这2个工具，我这里选择把他们放在同一目录中 方面shell脚本执行 并发必须,你可以按自己的目录结构自行调整命令，

> protoc.exe helloworld.proto --plugin=protoc-gen-grpc-web=protoc-gen-grpc-web-1.0.4-windows-x86_64.exe  --js_out=import_style=commonjs:.   --grpc-web_out=import_style=commonjs,mode=grpcwebtext:.

通过执行上面的命令后 得到 这2个js
1. helloworld_pb.js
2. helloworld_grpc_web_pb.js


### 编译 前端项目
```bash
$ npm install
$ npx webpack client.js
```

  编译后得到 输出的 
  > dist/main.js

### 使用 caddy webserver

有很多 webserver 可用。看重caddy 简单这里使用 caddy 来跑前端

Caddyfile 配置
```bash
localhost:8081 {
	root ../../src/grpcWebClient

}
```



## 最后
当然是放上源码 github 

