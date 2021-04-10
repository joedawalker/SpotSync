import React from "react";
import styled from "styled-components";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faPlayCircle, faStepForward, faPauseCircle } from "@fortawesome/free-solid-svg-icons";
import { connect } from "react-redux";
import { getUser, getPartyCode, getRealtimeConnection, getCurrentSong, getSongFeelings, isHost } from "../../../redux/reducers/reducers";
import { togglePlaybackState } from "../../../api/party";
import { skipSong } from "../../../api/partyHub";
import { userLikesSong, userDislikesSong } from "../../../api/partyHub";
import ThumbsUp from "../../shared/ThumbsUp";
import ThumbsDown from "../../shared/ThumbsDown";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.min.css";
import NoAlbumArt from "../../../assets/unknown-album-art.png";
import Image from "../../shared/Image";
const $NowPlaying = styled.div`
  box-sizing: border-box;
  width: 100%;
  bottom: 0px;
  left: 0;
  padding: 15px;
  display: flex;
  justify-content: space-around;

  flex: 0 1 30px;
`;

const $SongManagement = styled.div`
  display: flex;
  flex: 1;
  align-items: center;
  justify-content: center;
`;

const $VolumeManagement = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  marginright: auto;
  flex: 1;
`;

const $Volume = styled.input``;

const $PlayFontAwesomeIcon = styled(FontAwesomeIcon)`
  font-size: 30px;

  &:hover {
    color: grey;
  }
`;

const $SkipFontAwesomeIcon = styled(FontAwesomeIcon)`
  font-size: 20px;

  &:hover {
    color: grey;
  }
`;

const $NowPlayingSong = styled.div`
  display: flex;
  margin-right: auto;
  flex: 1;

  .song-information {
    display: flex;
    flex-direction: column;
    justify-content: center;
    padding: 10px;
  }
  p {
    margin: 0px;
  }

  img {
    width: 50px;
    height: 50px;
  }

  .title {
    font-size: 15px;
    font-weight: bold;
  }

  .artist {
    font-size: 12px;
  }
`;

const $ThumbsContainer = styled.div`
  display: flex;
  width: 50px;
  justify-content: space-around;
  align-items: center;
`;
const NowPlaying = ({ user, partyCode, dispatch, connection, currentSong, songFeelings, isHost }) => {
  return (
    <React.Fragment>
      <ToastContainer
        position="bottom-center"
        style={{ bottom: "100px" }}
        autoClose={5000}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
      ></ToastContainer>
      {partyCode && currentSong && (
        <$NowPlaying>
          <$NowPlayingSong>
            <React.Fragment>
              <Image src={currentSong?.albumImageUrl != undefined ? currentSong.albumImageUrl : NoAlbumArt} />
              <div className="song-information">
                <p className={"title"}>{currentSong?.name}</p>
                <p className={"artist"}>{currentSong?.artist}</p>
              </div>

              {songFeelings && currentSong && (
                <$ThumbsContainer>
                  <ThumbsDown
                    onDislike={() => userDislikesSong(partyCode, currentSong.uri, connection, dispatch)}
                    feeling={songFeelings[currentSong?.uri]}
                  />
                  <ThumbsUp onLike={() => userLikesSong(partyCode, currentSong.uri, connection, dispatch)} feeling={songFeelings[currentSong?.uri]} />
                </$ThumbsContainer>
              )}
            </React.Fragment>
          </$NowPlayingSong>
          <$SongManagement>
            {user?.details?.pausedMusic ? (
              <$PlayFontAwesomeIcon icon={faPlayCircle} onClick={() => togglePlaybackState(partyCode, dispatch)} />
            ) : (
              <$PlayFontAwesomeIcon icon={faPauseCircle} onClick={() => togglePlaybackState(partyCode, dispatch)} />
            )}
            {isHost && <$SkipFontAwesomeIcon icon={faStepForward} onClick={() => skipSong(partyCode, connection)}></$SkipFontAwesomeIcon>}
          </$SongManagement>
          <$VolumeManagement>
            <$Volume type="range" min="0" max="10" id="spotify-volume-slider" />
          </$VolumeManagement>
        </$NowPlaying>
      )}
    </React.Fragment>
  );
};

const mapStateToProps = (state) => {
  return {
    user: getUser(state),
    partyCode: getPartyCode(state),
    connection: getRealtimeConnection(state).connection,
    currentSong: getCurrentSong(state),
    songFeelings: getSongFeelings(state),
    isHost: isHost(state),
  };
};

export default connect(mapStateToProps, null)(NowPlaying);
