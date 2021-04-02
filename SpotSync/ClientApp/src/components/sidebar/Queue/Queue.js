import React from "react";
import QueueItem from "./QueueItem";
import { connect } from "react-redux";
import { getParty, getRealtimeConnection } from "../../../redux/reducers/reducers";
import Button from "../../shared/Button";
import { generateQueue } from "../../../api/party";
import { userLikesSong, userDislikesSong } from "../../../api/partyHub";
import Subtitle from "../../shared/Subtitle";
import CenteredHorizontally from "../../shared/CenteredHorizontally";

const Queue = ({ party, connection, songFeelings = {}, dispatch }) => {
  return party?.queue?.length > 0 ? (
    <React.Fragment>
      {party.queue.map((song) => {
        return (
          <QueueItem
            onLike={() => {
              userLikesSong(party.code, song.uri, connection, dispatch);
            }}
            onDislike={() => {
              userDislikesSong(party.code, song.uri, connection, dispatch);
            }}
            key={song.uri}
            title={song.name}
            artist={song.artist}
            feeling={songFeelings[song.uri]}
          ></QueueItem>
        );
      })}
    </React.Fragment>
  ) : party?.history?.length > 0 ? (
    <CenteredHorizontally>
      <Subtitle>A new queue will be generated from your liked songs next song.</Subtitle>
    </CenteredHorizontally>
  ) : (
    <React.Fragment>
      <Button onClick={() => generateQueue(party.code)}>Generate Playlist</Button>
    </React.Fragment>
  );
};

const mapStateToProps = (state) => {
  return { party: getParty(state), connection: getRealtimeConnection(state).connection };
};

export default connect(mapStateToProps, null)(Queue);
