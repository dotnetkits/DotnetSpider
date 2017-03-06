﻿using DotnetSpider.Core;
using DotnetSpider.Core.Common;
using DotnetSpider.Extension;
using DotnetSpider.Extension.Model;
using DotnetSpider.Extension.Model.Attribute;
using DotnetSpider.Extension.ORM;
using DotnetSpider.Extension.Pipeline;
using System;

namespace DotnetSpider.Demo
{
	public class Program
	{
		public class JdCategorySpider : EntitySpiderBuilder
		{
			[Schema("jd", "jd_category")]
			[EntitySelector(Expression = ".//div[@class='items']//a")]
			public class Category : ISpiderEntity
			{
				[StoredAs("name", DataType.String, 50)]
				[PropertySelector(Expression = ".")]
				public string CategoryName { get; set; }

				[StoredAs("url", DataType.Text)]
				[PropertySelector(Expression = "./@href")]
				public string Url { get; set; }
			}

			protected override EntitySpider GetEntitySpider()
			{
				var entitySpider = new EntitySpider(new Site())
				{
					Identity = "JdCategory Daliy Tracking " + DateTimeUtils.Day1OfThisWeek.ToString("yyyy-MM-dd")
				};

				entitySpider.AddStartUrl("http://www.jd.com/allSort.aspx");
				entitySpider.AddEntityType(typeof(Category));
				entitySpider.AddEntityPipeline(new MySqlEntityPipeline("Database='mysql';Data Source=localhost;User ID=root;Password=1qazZAQ!;Port=3306"));
				return entitySpider;
			}
		}

		public static void Main(string[] args)
		{
			JdCategorySpider spider = new JdCategorySpider();
			spider.Run();

			BaseUsage.CustmizeProcessorAndPipeline();
			Console.Read();
		}
	}
}
