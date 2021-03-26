export const PARTY_JOINED = "party_joined";
export const LEFT_PARTY = "left_party";
export const UPDATE_QUEUE = "update_queue";
export const UPDATE_HISTORY = "update_history";
export const SEARCHED_SPOTIFY = "SEARCHED_SPOTIFY";
export const TOGGLE_PLAYBACK = "toggle_playback";

export const partyLeft = () => {
  return {
    type: LEFT_PARTY,
  };
};

export const partyJoined = (partyCode) => {
  return {
    type: PARTY_JOINED,
    partyCode,
  };
};

export const updateQueue = (queue) => {
  return {
    type: UPDATE_QUEUE,
    queue,
  };
};

export const updateHistory = (history) => {
  return {
    type: UPDATE_HISTORY,
    history,
  };
};

export const saveSpotifySearchResults = (results) => {
  return {
    type: SEARCHED_SPOTIFY,
    results,
  };
};

export const togglePlayback = () => {
  return {
    type: TOGGLE_PLAYBACK,
  };
};
