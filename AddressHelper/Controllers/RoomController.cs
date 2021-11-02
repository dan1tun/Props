using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AddressHelper.Controllers
{
    [ApiController]
    [Route("/")]
    public class RoomController : ControllerBase
    {
        static Dictionary<String, String> rooms = new Dictionary<String, String>();

        private readonly ILogger<RoomController> _logger;

        public RoomController(ILogger<RoomController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string GetRoom(String key)
        {
            if (String.IsNullOrEmpty(key))
            {
                string roomsString = "[";
                foreach (var id in rooms.Keys)
                {
                    roomsString += $"id:'{id}', ip:'{rooms[id]}',";
                }
                roomsString += "]";
                return roomsString;
            }

            if (!rooms.ContainsKey(key))
                return null;

            return rooms[key];
        }

        [HttpPost]
        public string CreateRoom(String ipAddress)
        {
            if(rooms.ContainsValue(ipAddress))
                return "exists";
            string roomId = RandomString(4);
            int tries = 10;

            while (rooms.ContainsKey(roomId) && tries-->0)
                roomId = RandomString(4);

            if (tries <= 0)
                return "error";

            rooms.Add(roomId, ipAddress);
            return roomId;
        }

        [HttpDelete]
        public bool DeleteRoom(String key)
        {
            if (!rooms.ContainsKey(key))
                return false;

            try
            {
                rooms.Remove(key);
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        [HttpPut]
        public void Reset()
        {
            rooms = new Dictionary<String, String>();
        }


        private string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
