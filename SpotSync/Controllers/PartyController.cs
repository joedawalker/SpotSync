﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SpotSync.Classes.Hubs;
using SpotSync.Classes.Responses.Common;
using SpotSync.Classes.Responses.Party;
using SpotSync.Domain;
using SpotSync.Domain.Contracts;
using SpotSync.Domain.Contracts.Services;
using SpotSync.Domain.DTO;
using SpotSync.Domain.Errors;
using SpotSync.Domain.Events;
using SpotSync.Models.Dashboard;
using SpotSync.Models.Party;
using SpotSync.Models.Shared;
using Track = SpotSync.Domain.Track;

namespace SpotSync.Controllers
{
    public class PartyController : Controller
    {
        private readonly IPartyService _partyService;
        private readonly IPartyGoerService _partyGoerService;
        private readonly IHubContext<PartyHub> _partyHubContext;
        private readonly ILogService _logService;

        public PartyController(IPartyService partyService, IHubContext<PartyHub> hubContext, ILogService logService, IPartyGoerService partyGoerService)
        {
            _partyService = partyService;
            _partyGoerService = partyGoerService;
            _partyHubContext = hubContext;
            _logService = logService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> StartParty()
        {
            if (await _partyService.IsUserPartyingAsync(await _partyGoerService.GetCurrentPartyGoerAsync()))
            {
                return StatusCode(409);
            }

            string partyCode = await _partyService.StartPartyAsync();

            return Ok(new { partyCode = partyCode });
        }

        [Authorize]
        [HttpGet("api/[controller]/UsersLikesDislikes")]
        public async Task<IActionResult> GetUsersLikesDislikes(string partyCode)
        {
            try
            {
                Party party = await _partyService.GetPartyWithCodeAsync(partyCode);
                PartyGoer user = await _partyGoerService.GetCurrentPartyGoerAsync();

                return new JsonResult(new Result<LikedDislikedSongs>(ConvertUsersLikedDislikesToDto(party.GetUsersLikesDislikes(user))));
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred while trying to get users likes and dislikes");
                return new JsonResult(new Result(false, "Unable to get your liked and disliked songs"));
            }
        }

        private LikedDislikedSongs ConvertUsersLikedDislikesToDto(LikesDislikes likedDislikedSongs)
        {
            return new LikedDislikedSongs
            {
                LikedSongs = likedDislikedSongs.GetLikedSongs(),
                DislikedSongs = likedDislikedSongs.GetDislikedSongs()
            };
        }

        [Authorize]
        public async Task<IActionResult> TogglePlaybackState(string partyCode)
        {
            try
            {
                await _partyService.TogglePlaybackStateAsync(partyCode, await _partyGoerService.GetCurrentPartyGoerAsync());
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in TogglePlaybackState()");
                return new StatusCodeResult(500);
            }

            return new StatusCodeResult(200);
        }


        [Authorize]
        public async Task<IActionResult> Index(string partyCode)
        {
            PartyGoer user = new PartyGoer(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Party party;

            if (string.IsNullOrWhiteSpace(partyCode))
            {
                party = await _partyService.GetPartyWithAttendeeAsync(user);
            }
            else
            {
                party = await _partyService.GetPartyWithCodeAsync(partyCode);
            }

            if (party == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            List<Track> usersSuggestedSongs = null;

            bool isUserListening = party.IsListener(user);

            if (isUserListening)
            {
                usersSuggestedSongs = await _partyGoerService.GetRecommendedSongsAsync(user.Id);
            }

            PartyModel model = new PartyModel
            {
                PartyCode = party.GetPartyCode(),
                SuggestedSongs = usersSuggestedSongs?.Select(song => ConvertDomainSongToModelSong(song)).ToList(),
                IsUserListening = isUserListening
            };

            BaseModel baseModel = new BaseModel(true, model.PartyCode);
            return View(new BaseModel<PartyModel>(model, baseModel));
        }

        private SongModel ConvertDomainSongToModelSong(Track song)
        {
            return new SongModel
            {
                Title = song.Name,
                Artist = song.Artist,
                AlbumImageUrl = song.AlbumImageUrl,
                Length = song.Length,
                TrackUri = song.Uri
            };
        }

        /// <summary>
        /// This endpoint is allowed to be accessed if you are not authenticated. If you are not authenticated, then you will be redirected to login
        /// </summary>
        /// <param name="partyCode"></param>
        /// <returns></returns>
        public async Task<IActionResult> JoinParty(string partyCode)
        {
            PartyGoer user = await _partyGoerService.GetCurrentPartyGoerAsync();

            if (user == null)
            {
                return new JsonResult(new Result(false, "User is not authenticated"));
            }


            Party party = await _partyService.GetPartyWithCodeAsync(partyCode);

            if (party == null)
            {
                await _logService.LogUserActivityAsync(user.Id, $"Failed to join party, party code did was not a valid party: {partyCode}");

                return new JsonResult(new Result(false, "Party code was not valid"));
            }

            if (await _partyService.IsUserPartyingAsync(user))
            {
                // User can only join 1 party at a time
                return new JsonResult(new Result(false, "You cannot join 2 parties. You must leave the first to join another"));
            }

            try
            {
                party.JoinParty(user);

                await _logService.LogUserActivityAsync(user.Id, $"Joined a party with code {partyCode}");

                return new JsonResult(new Result<JoinedParty>(new JoinedParty { PartyCode = partyCode, SuccessfullyJoinedParty = true }));

            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, $"{user.Id} failed to join party {partyCode}");
            }

            return new JsonResult(new Result(false, "An error occurred while trying to join party"));
        }

        [Authorize]
        public async Task<IActionResult> LeaveParty(string partyCode)
        {
            if (partyCode == null)
            {
                RedirectToAction("Index", "Dashboard");
            }

            PartyGoer user = new PartyGoer(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (!await _partyService.LeavePartyAsync(user))
            {
                await _logService.LogUserActivityAsync(user.Id, $"User failed to leave party {partyCode}");
                return RedirectToAction("Index", "Dashboard");
            }

            await _logService.LogUserActivityAsync(user.Id, $"User successfully left party {partyCode}");

            return RedirectToAction("Index", "Dashboard"); ;
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> EndParty()
        {
            PartyGoer host = new PartyGoer(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (await _partyService.IsUserHostingAPartyAsync(host))
            {
                if (await _partyService.EndPartyAsync(host))
                {
                    await _logService.LogUserActivityAsync(host.Id, $"User successfully ended party");
                    return Ok();
                }
                else
                {
                    await _logService.LogUserActivityAsync(host.Id, $"User failed to end party");
                    return BadRequest("There was an issue with deleting your party");
                }
            }
            else
            {
                await _logService.LogUserActivityAsync(host.Id, $"User failed to end party because they weren't the host");
                return BadRequest("Unable to delete party. You are not hosting any parties");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateSongForParty([FromBody]PartyCodeDTO partyCode)
        {
            PartyGoer user = new PartyGoer(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (await _partyService.IsUserPartyingAsync(user))
            {
                Party party = await _partyService.GetPartyWithAttendeeAsync(user);

                await _logService.LogUserActivityAsync(user.Id, $"User updated song for party with code {partyCode.PartyCode}");

                return await UpdateCurrentSongForEveryoneInPartyAsync(party, user);
            }
            else
            {
                await _logService.LogUserActivityAsync(user.Id, $"User failed tp update song for party with code {partyCode.PartyCode}");
                return BadRequest($"You are currently not hosting a party or attending a party: {partyCode.PartyCode}");
            }
        }

        [Authorize]
        public async Task<IActionResult> UpdateSongForUser()
        {
            try
            {
                PartyGoer listener = new PartyGoer(User.FindFirstValue(ClaimTypes.NameIdentifier));

                await _partyService.SyncListenerWithSongAsync(listener);

                return StatusCode(200);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in UpdateSongForUser()");
                return StatusCode(500);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateQueueForParty(string partyCode)
        {
            PartyGoer user = new PartyGoer(User.FindFirstValue(ClaimTypes.NameIdentifier));

            Party party = await _partyService.GetPartyWithCodeAsync(partyCode);

            if (party.IsHost(user))
            {

                await UpdatePlaylistForEveryoneInPartyAsync(party, user);

                return Ok();
            }
            else
            {
                return new StatusCodeResult(401);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult App()
        {
            return View();
        }

        private async Task UpdatePlaylistForEveryoneInPartyAsync(Party party, PartyGoer partyGoer)
        {
            await DomainEvents.RaiseAsync(new PlaylistEnded { PartyCode = party.GetPartyCode(), LikedTracksUris = party.GetLikedTracksUris(5) });
        }

        private async Task<IActionResult> UpdateCurrentSongForEveryoneInPartyAsync(Party party, PartyGoer partyGoer)
        {
            try
            {
                await _partyService.UpdateCurrentSongForEveryoneInPartyAsync(party, partyGoer);

                return Ok();
            }
            catch
            {
                return base.StatusCode(500);
            }
        }

        [Authorize]
        public async Task<IActionResult> GetActiveParties()
        {
            List<TopPartyModel> topParties = ConvertToTopPartyModels(await _partyService.GetTopPartiesAsync(5));

            return Ok(topParties);
        }

        private List<TopPartyModel> ConvertToTopPartyModels(List<Party> parties)
        {
            List<TopPartyModel> topParties = new List<TopPartyModel>();

            foreach (Party party in parties)
            {
                topParties.Add(new TopPartyModel
                {
                    PartyCode = party.GetPartyCode(),
                    ListenerCount = party.GetListenerCount(),
                    CurrentSong = ConvertToSimpleTrackModel(party.GetCurrentSong())
                });
            }

            return topParties;
        }

        private SimpleTrackModel ConvertToSimpleTrackModel(Track track)
        {
            return new SimpleTrackModel
            {
                Title = track.Name,
                Artist = track.Artist
            };
        }
    }
}