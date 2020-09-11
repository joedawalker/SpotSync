﻿using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SpotSync.Domain.Contracts;
using SpotSync.Domain.DTO;
using SpotSync.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SpotSync.Tests.Unit_Tests
{
    class SpotifyHttpClientTests
    {
        private ISpotifyHttpClient _spotifyHttpClient;
        private Mock<ISpotifyAuthentication> _spotifyAuthentication;
        private Mock<IHttpClient> _httpClient;

        private const string PARTY_GOER_ID = "ValidPartyGoerId";

        [SetUp]
        public void SetUp()
        {
            _spotifyAuthentication = new Mock<ISpotifyAuthentication>();
            _httpClient = new Mock<IHttpClient>();

            _spotifyHttpClient = new SpotifyHttpClient(_spotifyAuthentication.Object, _httpClient.Object);
        }

        [Test]
        public async Task GetCurrentSong()
        {
            //  TODO: Finish figuring out best way to unit test the third party api
            CurrentSongDTO currentSongDto = new CurrentSongDTO
            {
                Album = "",
                AlbumArtUrl = "",
                Artist = "",
                ProgressMs = 0,
                Title = "",
                TrackUri = ""
            };
            HttpResponseMessage responseMessage = new HttpResponseMessage { Content = new StringContent("", Encoding.UTF8, "application/json") };
            _spotifyAuthentication.Setup(p => p.GetAuthenticationHeaderForPartyGoerAsync(PARTY_GOER_ID)).Returns(Task.FromResult(new AuthenticationHeaderValue("Bearer")));
            _httpClient.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("")
            }
            ));

            //var currentSong = await _spotifyHttpClient.GetCurrentSongAsync(PARTY_GOER_ID);

            Assert.Inconclusive();
        }
    }
}