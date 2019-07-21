using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using Ingos_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Ingos_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public TechController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private string GetConnString()
        {
            return configuration.GetConnectionString("Ingos_db");
        }

        // GET api/tech
        [HttpGet]
        public IActionResult Get()
        {
            string cs = GetConnString();

            using (var con = new SqlConnection(cs))
            {
                con.Open();

                string queryString = @"declare @result nvarchar(max);
                                        set @result = (select * from technology for json auto);
                                        select @result;";

                SqlCommand cmd = new SqlCommand(queryString, con);

                try
                {
                    return Ok(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message.Replace(Environment.NewLine, ""));
                }
            }
        }

        // GET api/tech/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/tech
        [HttpPost]
        public IActionResult Post([FromBody] JObject data)
        {

            if (data == null)
                return BadRequest("Post body is null");

            string cs = GetConnString();
            var name = (data["name"]).ToString();
            var description = (data["description"]).ToString();

            using (var con = new SqlConnection(cs))
            {
                con.Open();

                string queryString = @"declare @message varchar(100);

                                        if not exists(select * from technology where name = @name)
                                            begin
                                                insert technology (name, description) values (@name, @description);
                                                select * from technology where name = @name for json auto;
                                            end
                                        else
                                            begin
                                                set @message = 'Technology with name ' + @name + ' already exists';
                                                raiserror (@message, 16, 16);
                                            end";

                SqlCommand cmd = new SqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("description", description);

                try
                {
                    return Ok(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message.Replace(Environment.NewLine, ""));
                }
            }
        }

        // PUT api/tech/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/tech/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
