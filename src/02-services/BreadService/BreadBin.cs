﻿using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitQueue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BreadService
{
  class BreadBin
  {
    private static Queue _queue = new Queue("dinky-wallaby-rabbitmq.default.svc.cluster.local", "breadbin");

    static async Task Main(string[] args)
    {
      Console.WriteLine("### Bread bin service starting to listen");
      _queue.StartListening(HandleMessage);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }

    private volatile static int _inventory = 10;

    private static void HandleMessage(BasicDeliverEventArgs ea, string message)
    {
      var request = JsonConvert.DeserializeObject<Messages.BreadBinRequest>(message);
      var response = new Messages.BreadBinResponse();
      lock (_queue)
      {
        if (request.Returning)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - returned");
          _inventory++;
        }
        else if (_inventory > 0)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - filled");
          _inventory--;
          response.Success = true;
          _queue.SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, response);
        }
        else
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - no inventory");
          response.Success = false;
          _queue.SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, response);
        }
      }
    }
  }
}
