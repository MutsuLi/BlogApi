using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Blog.Api.AOP;
using Blog.Api.Extensions;
using Blog.Api.Filter;
using Blog.Common;
using Blog.Common.LogHelper;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static Blog.Api.SwaggerHelper.CustomApiVersion;

namespace Blog.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }
        // private static readonly ILog log =LogManager.GetLogger(typeof(GlobalExceptionsFilter));

        //private IServiceCollection _services;
        private List<Type> tsDIAutofac = new List<Type>();
        private static readonly ILog log = LogManager.GetLogger(typeof(GlobalExceptionsFilter));
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var basePath = AppContext.BaseDirectory;
            //builder.RegisterType<AdvertisementServices>().As<IAdvertisementServices>();

            #region 带有接口层的服务注入

            var servicesDllFile = Path.Combine(basePath, "Blog.Services.dll");
            var repositoryDllFile = Path.Combine(basePath, "Blog.Repository.dll");

            if (!(File.Exists(servicesDllFile) && File.Exists(repositoryDllFile)))
            {
                var msg = "Repository.dll和service.dll 丢失，因为项目解耦了，所以需要先F6编译，再F5运行，请检查 bin 文件夹，并拷贝。";
                // log.Error(msg);
                throw new Exception(msg);
            }

            // AOP 开关，如果想要打开指定的功能，只需要在 appsettigns.json 对应对应 true 就行。
            var cacheType = new List<Type>();

            if (Appsettings.app(new string[] { "AppSettings", "RedisCachingAOP", "Enabled" }).ObjToBool())
            {
                builder.RegisterType<BlogRedisCacheAOP>();
                cacheType.Add(typeof(BlogRedisCacheAOP));
            }
            if (Appsettings.app(new string[] { "AppSettings", "MemoryCachingAOP", "Enabled" }).ObjToBool())
            {
                builder.RegisterType<BlogCacheAOP>();
                cacheType.Add(typeof(BlogCacheAOP));
            }
            // if (Appsettings.app(new string[] { "AppSettings", "TranAOP", "Enabled" }).ObjToBool())
            // {
            //     builder.RegisterType<BlogTranAOP>();
            //     cacheType.Add(typeof(BlogTranAOP));
            // }
            if (Appsettings.app(new string[] { "AppSettings", "LogAOP", "Enabled" }).ObjToBool())
            {
                builder.RegisterType<BlogLogAOP>();
                cacheType.Add(typeof(BlogLogAOP));
            }

            // 获取 Service.dll 程序集服务，并注册
            var assemblysServices = Assembly.LoadFrom(servicesDllFile);
            builder.RegisterAssemblyTypes(assemblysServices)
                      .AsImplementedInterfaces()
                      .InstancePerDependency()
                      .EnableInterfaceInterceptors()//引用Autofac.Extras.DynamicProxy;
                      .InterceptedBy(cacheType.ToArray());//允许将拦截器服务的列表分配给注册。

            // 获取 Repository.dll 程序集服务，并注册
            var assemblysRepository = Assembly.LoadFrom(repositoryDllFile);
            builder.RegisterAssemblyTypes(assemblysRepository)
                   .AsImplementedInterfaces()
                   .InstancePerDependency();

            #endregion

            #region 没有接口层的服务层注入

            //因为没有接口层，所以不能实现解耦，只能用 Load 方法。
            //注意如果使用没有接口的服务，并想对其使用 AOP 拦截，就必须设置为虚方法
            //var assemblysServicesNoInterfaces = Assembly.Load("Blog.Core.Services");
            //builder.RegisterAssemblyTypes(assemblysServicesNoInterfaces);

            #endregion

            // #region 没有接口的单独类，启用class代理拦截

            // //只能注入该类中的虚方法，且必须是public
            // builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(Love)))
            //     .EnableClassInterceptors()
            //     .InterceptedBy(cacheType.ToArray());
            // #endregion

            #region 单独注册一个含有接口的类，启用interface代理拦截

            //不用虚方法
            //builder.RegisterType<AopService>().As<IAopService>()
            //   .AsImplementedInterfaces()
            //   .EnableInterfaceInterceptors()
            //   .InterceptedBy(typeof(BlogCacheAOP));
            #endregion


            // 这里和注入没关系，只是获取注册列表，请忽略
            tsDIAutofac.AddRange(assemblysServices.GetTypes().ToList());
            tsDIAutofac.AddRange(assemblysRepository.GetTypes().ToList());
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new Appsettings(Configuration));
            services.AddSingleton(new LogLock(Env.ContentRootPath));
            services.AddScoped<IRedisCacheManager, RedisCacheManager>();
            services.AddMemoryCacheSetup();
            services.AddMiniProfilerSetup();
            services.AddDbSetup();
            services.AddSwaggerSetup();
            services.AddSqlsugarSetup();
            services.AddControllers();
            services.AddAutoMapperSetup();
            services.AddAuthorizationSetup();

            services.AddControllers(o =>
            {
                // 全局异常过滤
                o.Filters.Add(typeof(GlobalExceptionsFilter));
                // 全局路由权限公约
                //o.Conventions.Insert(0, new GlobalRouteAuthorizeConvention());
                // 全局路由前缀，统一修改路由
                //o.Conventions.Insert(0, new GlobalRoutePrefixFilter(new RouteAttribute(RoutePrefix.Name)));
            })
            //全局配置Json序列化处理
            .AddNewtonsoftJson(options =>
            {
                //忽略循环引用
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                //不使用驼峰样式的key
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                //设置时间格式
                //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {

            // // Ip限流,尽量放管道外层
            // app.UseIpRateLimiting();
            // // 记录所有的访问记录
            // loggerFactory.AddLog4Net();
            // // 记录请求与返回数据 
            // app.UseReuestResponseLog();
            // // signalr 
            // app.UseSignalRSendMildd();
            // // 记录ip请求
            // app.UseIPLogMildd();
            // // 查看注入的所有服务
            // app.UseAllServicesMildd(_services, tsDIAutofac);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.    
            app.UseSwaggerUI(c =>
            {
                var ApiName = Appsettings.app(new string[] { "Startup", "ApiName" });
                typeof(ApiVersions).GetEnumNames().OrderByDescending(e => e).ToList().ForEach(version =>
                {
                    c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{ApiName} {version}");
                });

                // 将swagger首页，设置成我们自定义的页面，记得这个字符串的写法：解决方案名.index.html
                c.IndexStream = () => GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Blog.Api.index.html");

                if (GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Blog.Api.index.html") == null)
                {
                    var msg = "index.html的属性，必须设置为嵌入的资源";
                    log.Error(msg);
                    throw new Exception(msg);
                }

                // 路径配置，设置为空，表示直接在根域名（localhost:8001）访问该文件,注意localhost:8001/swagger是访问不到的，去launchSettings.json把launchUrl去掉，如果你想换一个路径，直接写名字即可，比如直接写c.RoutePrefix = "doc";
                c.RoutePrefix = "";
            });


            app.UseCors("LimitRequests");

            // 跳转https
            //app.UseHttpsRedirection();
            // 使用静态文件
            app.UseStaticFiles();
            // 使用cookie
            app.UseCookiePolicy();
            // 返回错误码
            app.UseStatusCodePages();//把错误码返回前台，比如是404
            // Routing
            app.UseRouting();
            // 这种自定义授权中间件，可以尝试，但不推荐
            // app.UseJwtTokenAuth();
            // 先开启认证
            app.UseAuthentication();
            // 然后是授权中间件
            app.UseAuthorization();


            app.UseMiniProfiler();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
