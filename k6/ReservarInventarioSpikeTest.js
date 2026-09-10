import http from "k6/http";
import { sleep, check } from "k6";

const token = "";
const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  // Key configurations for spike in this section
  stages: [
    { duration: "20s", target: 500 }, // fast ramp-up to a high point
    // No plateau
    { duration: "1m", target: 0 }, // quick ramp-down to 0 users
  ],
};

export default () => {
  const urlRes = http.post(
    `${BASE_URL}/ordem/create`,
    JSON.stringify({
      pedido: {
        "9f9829ed-490a-4dd3-985e-e2f4b8c3977c": 1,
      },
    }),
    {
      headers: {
        "Content-Type": "application/json",
        accept: "*/*",
        Authorization: `Bearer ${token}`,
      },
    },
  );

  check(urlRes, {
    "status was 200": (r) => {
      return r.status == 200;
    },
  });
  sleep(1);
};
