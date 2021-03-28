import { setupSignalRConnection } from "./setupSignalRConnection";

import { useDispatch } from "react-redux";

const connectionHub = "/partyhub";
const getAccessToken = (state) => {
  return state.user.accessToken;
};
export const setupPartyHub = (dispatch) => setupSignalRConnection(connectionHub, {}, getAccessToken)(dispatch);

export default () => () => {
  const dispatch = useDispatch();
  //dispatch(setupEventsHub); // dispatch is coming from Redux
};