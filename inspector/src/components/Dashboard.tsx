import { Box, LinearProgress } from "@mui/material";
import { useState } from "react";
import { Bar } from "react-chartjs-2";
import { Envelope, useInspectorStream } from "../streams/WebSocketHelper";
import type { ChartData, ChartOptions } from "chart.js";
import { Chart as ChartJS, registerables } from "chart.js";
ChartJS.register(...registerables);

const options: ChartOptions<"bar"> = {
  indexAxis: "y" as const,
  scales: {
    x: {
      type: "linear",
      position: "top",
    },
  },
  elements: {
    bar: {
      borderWidth: 2,
    },
  },
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: false,
    },
    title: {
      display: true,
      text: "# Entities in groups",
    },
  },
};

export function Dashboard() {
  const [data, setData] = useState<
    ChartData<"bar", number[], unknown> | undefined
  >(undefined);

  const { sendMessage } = useInspectorStream({
    onMessageReceived: (e: Envelope<any>) => {
      if (e.Id !== "dashboard") return;

      const groupCounts = new Map<string, number>(
        Object.entries(e.Payload.Groups)
      );

      const entries = Array.from(groupCounts.entries()).sort(
        (a, b) => b[1] - a[1]
      );

      const data = {
        labels: entries.map((x) => x[0].split(" ")),
        datasets: [
          {
            label: "# entities",
            data: entries.map((x) => x[1]),
            borderColor: "rgb(255, 99, 132)",
            backgroundColor: "rgba(255, 99, 132, 0.5)",
          },
        ],
      };

      setData(data);
    },
    onOpen: () => {
      sendMessage("sub dashboard");
    },
  });

  if (data === undefined) return <LinearProgress />;

  return (
    <Box width={"800px"} height={(data.labels?.length ?? 1) * 70}>
      <Bar options={options} data={data} />
    </Box>
  );
}
