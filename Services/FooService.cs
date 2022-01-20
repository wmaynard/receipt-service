using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace Rumble.Platform.ReceiptService.Services
{
	// TODO: Remove this .cs file once the dev environment is verified to be working.
	public class FooService : PlatformMongoService<Foo>
	{
		public FooService() : base("foo") { }
	}

	public class Foo : PlatformCollectionDocument
	{
		public string Guid { get; set; }
	}

	[ApiController, Route(template: "commerce/receipt/foo")]
	public class FooController : PlatformController
	{
		private readonly FooService _fooService;
		
		public FooController(FooService service, IConfiguration config) : base(config) => _fooService = service;

		[HttpGet, Route("test")]
		public ActionResult Test()
		{
			Foo foo = new Foo()
			{
				Guid = Guid.NewGuid().ToString()
			};
			_fooService.Create(foo);
			return Ok(new { Foo = foo });
		}
		
		[HttpGet, Route("health")]
		public override ActionResult HealthCheck()
		{
			return Ok(_fooService.HealthCheckResponseObject);
		}
	}
}