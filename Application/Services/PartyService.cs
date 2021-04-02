﻿using SpotSync.Domain;
using SpotSync.Domain.Contracts;
using SpotSync.Domain.Contracts.Services;
using SpotSync.Domain.Contracts.Services.PartyGoerSetting;
using SpotSync.Domain.DTO;
using SpotSync.Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotSync.Application.Services
{
    public class PartyService : IPartyService
    {
        private IPartyRepository _partyRepository;
        private IPartyGoerService _partyGoerService;
        private ISpotifyHttpClient _spotifyHttpClient;
        private ILogService _logService;
        private IPartyGoerDetailsService _partyGoerDetailsService;

        public PartyService(IPartyRepository partyRepository, ISpotifyHttpClient spotifyHttpClient, ILogService logService, IPartyGoerService partyGoerService, IPartyGoerDetailsService partyGoerDetailsService)
        {
            _partyRepository = partyRepository;
            _spotifyHttpClient = spotifyHttpClient;
            _partyGoerService = partyGoerService;
            _logService = logService;
            _partyGoerDetailsService = partyGoerDetailsService;
        }

        public async Task<List<Party>> GetAllPartiesAsync()
        {
            return await _partyRepository.GetAllPartiesAsync();
        }

        public async Task TogglePlaybackStateAsync(string partyCode, PartyGoer partyGoer)
        {
            try
            {
                Party party = await _partyRepository.GetPartyWithCodeAsync(partyCode);

                await party.TogglePlaybackAsync(partyGoer);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in TogglePlaybackStateAsync()");
            }
        }

        public async Task UserWantsToSkipSong(PartyGoer partyGoer, string partyCode)
        {
            try
            {
                Party party = await _partyRepository.GetPartyWithCodeAsync(partyCode);

                await party.RequestSkipAsync(partyGoer);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in UserWantsToSkipSong()");
            }
        }

        public async Task<bool> AddNewSongToQueue(AddSongToQueueRequest request)
        {
            try
            {
                Party party = await _partyRepository.GetPartyWithCodeAsync(request.PartyCode);

                await party.AddTrackToQueueAsync(request);

                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occured in AddNewSongToQueue");
                return false;
            }
        }

        public async Task<Party> GetPartyWithCodeAsync(string partyCode)
        {
            if (string.IsNullOrWhiteSpace(partyCode))
            {
                return null;
            }

            try
            {
                return await _partyRepository.GetPartyWithCodeAsync(partyCode);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in GetPartyWithCodeAsync()");
                return null;
            }
        }

        public async Task SyncListenerWithSongAsync(PartyGoer listener)
        {

            Party party = await _partyRepository.GetPartyWithAttendeeAsync(listener);

            if (party != null && party.IsPartyPlayingMusic())
            {
                await party.SyncListenerWithSongAsync(listener);
            }
        }

        public async Task<string> StartPartyAsync()
        {
            Party party = new Party(await _partyGoerService.GetCurrentPartyGoerAsync());

            _partyRepository.CreateParty(party);

            return party.GetPartyCode();
        }

        public async Task<bool> IsUserHostingAPartyAsync(PartyGoer host)
        {
            try
            {
                return _partyRepository.IsUserHostingAParty(host);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in IsUserHostiongAPartyAsync");
                return false;
            }
        }

        public async Task<bool> EndPartyAsync(PartyGoer host)
        {
            try
            {
                Party party = await _partyRepository.GetPartyWithHostAsync(host);

                if (party != null)
                {
                    return await _partyRepository.DeleteAsync(party.GetPartyCode());
                }

                return false;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in EndPartyAsync");
                return false;
            }
        }

        public async Task<bool> EndPartyAsync(string partyCode)
        {
            try
            {
                return await _partyRepository.DeleteAsync(partyCode);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in EndPartyAsync");
                return false;
            }
        }

        public async Task<List<Track>> GenerateNewPlaylist(Party party, List<string> recommendedTrackUris)
        {
            if (recommendedTrackUris.Count > 0)
            {
                return await _spotifyHttpClient.GetRecommendedSongsAsync(party.GetHost().Id, new GetRecommendedSongs
                {
                    SeedTrackUris = recommendedTrackUris,
                    Market = _partyGoerDetailsService.GetMarket(party.GetHost())
                });
            }
            else
            {
                return await _spotifyHttpClient.GetRecommendedSongsAsync(party.GetHost().Id, new GetRecommendedSongs
                {
                    SeedTrackUris = (await _partyGoerService.GetRecommendedSongsAsync(party.GetHost().Id, 5)).Select(track => track.Uri).ToList(),
                    Market = _partyGoerDetailsService.GetMarket(party.GetHost())
                });
            }

        }


        public async Task<bool> JoinPartyAsync(PartyCodeDTO partyCode, PartyGoer attendee)
        {
            try
            {
                Party party = await _partyRepository.GetPartyWithCodeAsync(partyCode.PartyCode);

                if (party == null)
                {
                    return false;
                }

                return await JoinPartyAsync(partyCode, attendee);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in JoinPartyAsync");
                return false;
            }
        }

        public async Task<string> StartNewPartyAsync(PartyGoer partyHost)
        {
            try
            {
                Party party = new Party(partyHost);

                /*party.CreatePlaylist(new Playlist(new List<Song> { new Song { Length = 4000 }, new Song { Length = 8000 }, new Song { Length = 1000 } }));
                party.StartPlaylist();
                */

                _partyRepository.CreateParty(party);

                return party.GetPartyCode();
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in StartNewPartyAsync");
                return null;
            }
        }

        public async Task UpdateCurrentSongForEveryoneInPartyAsync(Party party, PartyGoer user)
        {
            try
            {
                if (party is null)
                {
                    throw new Exception($"Obtaining a party with code {party.GetPartyCode()} returned null from the database");
                }

                await party.UpdateCurrentSongForPartyAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in UpdateCurrentSongForEveryoneInPartyAsync");
            }
        }

        public async Task<Party> GetPartyWithHostAsync(PartyGoer host)
        {
            try
            {
                return await _partyRepository.GetPartyWithHostAsync(host);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in GetPartywithHostAsync");
                return null;
            }
        }

        public async Task<bool> IsUserPartyingAsync(PartyGoer user)
        {
            try
            {
                return await _partyRepository.IsUserInAPartyAsync(user);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in IsUserPartyingAsync");
                return false;
            }
        }

        public async Task<Party> GetPartyWithAttendeeAsync(PartyGoer attendee)
        {
            try
            {
                return await _partyRepository.GetPartyWithAttendeeAsync(attendee);
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in GetPartyWithAttendeeAsync");
                return null;
            }
        }

        public async Task<bool> LeavePartyAsync(PartyGoer attendee)
        {
            try
            {
                if (await IsUserPartyingAsync(attendee))
                {
                    _partyRepository.LeaveParty(attendee);
                }

                if (await IsUserHostingAPartyAsync(attendee))
                {
                    await _partyRepository.RemoveHostFromPartyAsync(attendee);
                }

                return true;

            }
            catch (PartyGoerWasInMultiplePartiesException ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in LeavePartyAsync");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, "Error occurred in LeavePartyAsync");
                return false;
            }
        }

        public async Task<List<Party>> GetTopPartiesAsync(int count)
        {
            // Currently this gets the parties with the most listeners
            return await _partyRepository.GetPartiesWithMostListenersAsync(count);
        }
    }
}
