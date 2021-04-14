﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SAM_Backend.Models;
using SAM_Backend.Services;
using SAM_Backend.Utility;
using SAM_Backend.ViewModels.Account;
using SAM_Backend.ViewModels.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAM_Backend.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly ILogger<AccountController> logger;
        private readonly IJWTService jWTService;
        private readonly AppDbContext context;
        private readonly IMinIOService minIOService;

        public RoomController(ILogger<AccountController> logger, IJWTService jWTService, AppDbContext context, IMinIOService minIOService)
        {
            this.logger = logger;
            this.jWTService = jWTService;
            this.context = context;
            this.minIOService = minIOService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(CreateRoomViewModel model)
        {
            #region validation
            var user = await jWTService.FindUserByTokenAsync(Request, context);
            var startDate = model.StartDate != null ? model.StartDate.Value : DateTime.Now;
            var endDate = model.EndDate != null ? model.EndDate.Value : DateTime.Parse(startDate.ToString()).Add(TimeSpan.FromHours(Constants.RoomDefaultExpirationPeriodInHours));
            if (endDate > startDate) return BadRequest("End date is sooner than start date!");
            var updatedInterests = model.Interests;
            if (!(InterestsService.IsValidRoomInterest(updatedInterests))) return BadRequest("Interests list is not in a valid format for a Room");
            #endregion

            #region room 
            var room = new Room()
            {
                Creator = user,
                StartDate = startDate,
                EndDate = endDate,
                Name = model.Name,
                Description = model.Description,
                Members = new List<AppUser>(),
            };
            InterestsService.SetInterestsForRoom(updatedInterests, room);
            #endregion room

            #region return
            context.Rooms.Add(room);
            context.SaveChanges();
            return Ok(new RoomViewModel(room));
            #endregion return
        }

        [HttpPost]
        public async Task<IActionResult> JoinRoom(int roomId)
        {
            #region find user & room
            var user = await jWTService.FindUserByTokenAsync(Request, context);
            var room = context.Rooms.Find(roomId);
            if (room == null) return NotFound(Constants.RoomNotFound);
            #endregion

            #region check membership
            if (room.Members.Contains(user)) return BadRequest("User is already a member of the room!");
            #endregion

            #region return
            room.Members.Add(user);
            user.InRooms.Add(room);
            context.SaveChanges();
            return Ok(new AppUserViewModel(user));
            #endregion
        }
    }
}
