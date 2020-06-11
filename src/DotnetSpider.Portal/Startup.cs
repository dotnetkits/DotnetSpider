﻿using System;
using System.IO;
using System.Reflection;
using Dapper;
using DotnetSpider.Kafka;
using DotnetSpider.Portal.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotnetSpider.Portal
{
	public class Startup
	{
		private readonly PortalOptions _options;

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			_options = new PortalOptions(Configuration);
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDotnetSpider(builder =>
			{
				builder.UserKafka();
				builder.AddDownloadCenter(x => x.UseMySqlDownloaderAgentStore());
				builder.AddSpiderStatisticsCenter(x => x.UseMemory());
			});

			services.AddMvc();//.SetCompatibilityVersion(CompatibilityVersion.Latest);

			services.AddScoped<PortalOptions>();
			// Add DbContext            
			Action<DbContextOptionsBuilder> dbContextOptionsBuilder;
			var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
			switch (_options.Database?.ToLower())
			{
				case "mysql":
				{
					dbContextOptionsBuilder = b =>
						b.UseMySql(_options.ConnectionString,
							sql => sql.MigrationsAssembly(migrationsAssembly));
					break;
				}
				default:
				{
					dbContextOptionsBuilder = b =>
						b.UseSqlServer(_options.ConnectionString,
							sql => sql.MigrationsAssembly(migrationsAssembly));
					break;
				}
			}

			services.AddDbContext<PortalDbContext>(dbContextOptionsBuilder);
			services.AddQuartz();
			services.AddHostedService<QuartzService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			Ioc.ServiceProvider = app.ApplicationServices;
			
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				// app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouter(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});

			using (IServiceScope scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>()
				.CreateScope())
			{
				var context = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
				context.Database.Migrate();
				context.Database.EnsureCreated();

				string sql;
				switch (_options.Database?.ToLower())
				{
					case "mysql":
					{
						using (var reader = new StreamReader(GetType().Assembly
							.GetManifestResourceStream("DotnetSpider.Portal.DDL.MySql.sql")))
						{
							sql = reader.ReadToEnd();
						}

						break;
					}
					default:
					{
						using (var reader = new StreamReader(GetType().Assembly
							.GetManifestResourceStream("DotnetSpider.Portal.DDL.SqlServer.sql")))
						{
							sql = reader.ReadToEnd();
						}

						break;
					}
				}

				using (var conn = context.Database.GetDbConnection())
				{
					conn.Execute(sql);
				}
			}
		}
	}
}