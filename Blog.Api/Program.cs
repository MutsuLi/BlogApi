using System;
using Autofac.Extensions.DependencyInjection;
using Blog.Model.Models;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blog.Api
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        public static void Main(string[] args)
        {
            // ���ɳ��� web Ӧ�ó���� Microsoft.AspNetCore.Hosting.IWebHost��Build��WebHostBuilder���յ�Ŀ�ģ�������һ�������WebHost����������������
            var host = CreateHostBuilder(args).Build();

            // ���������ڽ��������������� Microsoft.Extensions.DependencyInjection.IServiceScope��
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();

                try
                {
                    // �� system.IServicec�ṩ�����ȡ T ���͵ķ���
                    // ���ݿ������ַ������� Model ��� Seed �ļ����µ� MyContext.cs ��
                    var configuration = services.GetRequiredService<IConfiguration>();
                    if (configuration.GetSection("AppSettings")["SeedDBEnabled"].ObjToBool() || configuration.GetSection("AppSettings")["SeedDBDataEnabled"].ObjToBool())
                    {
                        var myContext = services.GetRequiredService<DbContext>();
                        var Env = services.GetRequiredService<IWebHostEnvironment>();
                        DBSeed.SeedAsync(myContext, Env.WebRootPath).Wait();
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Error occured seeding the Database.\n{e.Message}");
                    throw;
                }
            }

            // ���� web Ӧ�ó�����ֹ�����߳�, ֱ�������رա�
            // ������ WebHost ֮�󣬱�������� Run �������� Run ������ȥ���� WebHost �� StartAsync ����
            // ��Initialize����������Application�ܵ������Թ�������Ϣ
            // ִ��HostedServiceExecutor.StartAsync����
            // �������� ���쳣���鿴 Log �ļ����µ��쳣��־ ��������  
            host.Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory()) //<--NOTE THIS
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                .UseStartup<Startup>()
                .ConfigureLogging((hostingContext, builder) =>
                {
                    //�÷�����Ҫ����Microsoft.Extensions.Logging���ƿռ�
                    builder.AddFilter("System", LogLevel.Error); //���˵�ϵͳĬ�ϵ�һЩ��־
                    builder.AddFilter("Microsoft", LogLevel.Error);//���˵�ϵͳĬ�ϵ�һЩ��־

                    //���Log4Net
                    //var path = Directory.GetCurrentDirectory() + "\\log4net.config"; 
                    //������������ʾlog4net.config�������ļ�����Ӧ�ó����Ŀ¼�£�Ҳ����ָ�������ļ���·��
                    //��Ҫ���nuget����Microsoft.Extensions.Logging.Log4Net.AspNetCore
                    builder.AddLog4Net();
                });
            });
    }
}
