﻿using Azure.Storage.Files.Shares;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;


Console.OutputEncoding = System.Text.Encoding.UTF8;
var host = CreateHostBuilder().Build();
using (var serviceScope = host.Services.CreateScope())
{
    var serviceProvider = serviceScope.ServiceProvider;
    try
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        var actions = typeof(Program).Assembly.GetTypes().Where(t => t.GetCustomAttribute(typeof(VerbAttribute)) != null).OrderBy(t => t.Name).ToList();
        var parserResult = Parser.Default.ParseArguments(args, actions.ToArray());
        var parsed = parserResult as Parsed<object>;

        if (parsed != null)
        {
            var action = parsed.Value;
            var actionInvocationMethod = action.GetType().GetMethods().Single(m => !m.IsSpecialName && !m.IsStatic && m.DeclaringType == action.GetType());
            try
            {
                var methodArguments = actionInvocationMethod.GetParameters().Select(p => serviceProvider.GetRequiredService(p.ParameterType)).ToArray();
                var result = actionInvocationMethod.Invoke(action, methodArguments);
                if (result is Task t)
                {
                    await t;
                }
            }
            catch (TargetInvocationException e)
            {
                logger.LogError(e, "Cannot invoke command.");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unkown error.");
            }
            return 0;
        }
        else
        {
            return 1;
        }
    }
    catch (Exception e)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(e, "Something went wrong.");
        return 1;
    }
}



static IHostBuilder CreateHostBuilder()
{
    return Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(cfg =>
        {
            cfg.AddJsonFile("appsettings.Development.json", optional: true);
        })
        .UseSerilog((ctx, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration);
        })
        .ConfigureServices((ctx, services) =>
        {
            var config = ctx.Configuration;

            services.AddTransient(sp => new ShareClient(config.GetConnectionString("StorageAccount"), shareName: "acishare"));
        });
}