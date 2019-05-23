using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace grpcWebDemo
{
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
}
