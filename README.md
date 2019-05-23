# grpc-web Envoy .net core3 grpcServer 

## ��¼ ǰ�� js ͨ�� Envoy ���������� ���� .net core3 grpcServer ʵ�����

![��������ͼ](https://user-gold-cdn.xitu.io/2019/1/28/16892894a56a9773?imageView2/0/w/1280/h/960/format/webp/ignore-error/1)


## ����
* nodejs npm ��Ҫǰ�˱�������Ҫ
* .net core 3 sdk pv3
* vs2019 pv
* docker ubuntu18.04
* caddyServer 1.0
* chrome74


## ʵ��Ԥ��Ч�� 
* grpc-web clinet һ������ һ����Ӧ����
* grpc-web clinet ServerSteam һ������ Server ��ʽ�����Ӧ

## �˵����� ���𻷾�


| �˵�       | url            | ����       |
|----------|----------------|----------|
| grpc-web | localhost:8081 | web ǰ��ҳ�� |
| EnvoyServer    |loclhost:8080|����-docker ����           |
| grpcServer netcore3|loclhost:9090| rpc�ӿ� docker ����         |



### grpcServer �˵㹹��

ͨ�� vs2019 grpcģ��  ����һ��grpcServer��Ŀ
���ģ��  �Ѿ��Դ��˸��� helloworld rpc������ �������� ���޸��� .proto ����㶨�� ����ServerSteam rpc ��������

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

2��rpc �˵�ʵ�ִ���
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

��Ϊ�Ǽ���ʾ û���� tls ֤�� �� https
��Ŀ������ docker��ʽ ���� �������������޸�����Ĭ�� Kestrel �˿� ��Ϊ�� <u>**9090**</u>

vs �������Ŀ�� docker ֧�� ѡ linux ƽ̨�� vs ��������� ��Ŀ����� dockerfile
ps: ������vsĬ������ dockerfile build ��ʱ�򱨴� ��ʾ·�����ԡ�û�������о� ���޸� dockerfile ����� ·�� �� �Ϳ��Ա�����

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

���������� ���ű�����һ�� �ո����ɵ� dockerfile ���û�б��� �Ϳ������ȷ��� �Ȼ���� 
docker-compose �� Envoy �� netcore grpc ������һ�� docker ������

> docker build -t helloworld/grpcnetcoredemo -f Dockerfile .

### Envoy ����
��Ϊ Envoy �ٷ�û���ṩ �����Ƶ� ��װ�� ����ͨ�� grpc-web �����Ĳ���ָ�� ��Envoy������ docker��

����  envoy.yaml �� Envoy Server �������ļ� ����������Ҫ ����һ�µ��� **����**(netcore grpc api) ��������ַ���е����� ��Ҫ�����Լ��Ļ������޸�-������������ð��� ���������� ��д

>    hosts: [{ socket_address: { address: netcore-server, port_value: 9090 }}]

��������ʹ�� ��netcore-server�� ����Ҫ˵���� docker-compose �е� �������������ʵ�ʵ�ַ

docker-compose.yml ��������

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

�������� ���� ��û��ͨ�� ִ��
> docker-compose up -d netcore-server envoy 

���ܰ� 2 �� ���� �������ˡ� �˿� �ֱ��� 8080 �� 9090 2��docker ���� 
������ ����˵� envoy ���� �� grpc netcore-server  �Ѿ��ֱ������


**�ڽ��� ǰ��grpc-web ֮ǰ �����Ȳ����� 2������**

envoy ���� ����Ҫ��ע ���ζ˵� ip  ��ַ������һ�С�
grpc netcore-server �������� 9090 ��������� ������c#�� grpc Clinet ����ֱ�ӵ����¸ղ�д�� 2��grpc �ӿ�
�����Ƿ�ɹ���


## grpc-web

grpcWebClient ǰ����Ŀ�ļ���


### .proto �Զ����� js�ļ�
������Ҫ�� netcore-server �˵��е� .proto �����Э���ļ� ͨ���������� xx-pb.js �ļ�

* protoc [release](https://github.com/protocolbuffers/protobuf/releases)
* protoc-gen-grpc-web [protoc plugin] [releases](https://github.com/grpc/grpc-web/releases)

�ֱ��ѹ ������2�����ߣ�������ѡ������Ƿ���ͬһĿ¼�� ����shell�ű�ִ�� ��������,����԰��Լ���Ŀ¼�ṹ���е������

> protoc.exe helloworld.proto --plugin=protoc-gen-grpc-web=protoc-gen-grpc-web-1.0.4-windows-x86_64.exe  --js_out=import_style=commonjs:.   --grpc-web_out=import_style=commonjs,mode=grpcwebtext:.

ͨ��ִ������������ �õ� ��2��js
1. helloworld_pb.js
2. helloworld_grpc_web_pb.js


### ���� ǰ����Ŀ
```bash
$ npm install
$ npx webpack client.js
```

  �����õ� ����� 
  > dist/main.js

### ʹ�� caddy webserver

�кܶ� webserver ���á�����caddy ������ʹ�� caddy ����ǰ��

Caddyfile ����
```bash
localhost:8081 {
	root ../../src/grpcWebClient

}
```



## ���
��Ȼ�Ƿ���Դ�� github 

