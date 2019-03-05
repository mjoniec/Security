﻿using Data.Services;
using Microsoft.Azure.Mobile.Server.Config;
using System.Web.Http;

namespace Gold.MobileApp.Controllers
{
    [MobileAppController]
    public class GoldController : ApiController
    {
        IGoldService _goldService;

        public GoldController(IGoldService goldService)
        {
            _goldService = goldService;
        }

        [HttpGet]
        public IHttpActionResult Get()
        {
            return Ok("gold data");
        }
    }
}