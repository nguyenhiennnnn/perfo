import { useCallback, useRef, useState } from "react";
import { Ticket } from "lucide-react";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useCheckin } from "../hooks/use-checkin";
import { useCheckinSound } from "../hooks/use-checkin-sound";
import { classifyCheckinError } from "../lib/classify-checkin-error";
import type { CheckinFailure, CheckinResponse } from "../types";
import { CheckinCodeInput } from "../components/checkin-code-input";
import { CheckinCameraPanel } from "../components/checkin-camera-panel";
import { CheckinResultCard } from "../components/checkin-result-card";

type ScanSource = "camera" | "manual";

type CheckinOutcome =
  | { status: "idle" }
  | { status: "success"; result: CheckinResponse }
  | { status: "error"; failure: CheckinFailure };

const AdminCheckinPage = () => {
  const [code, setCode] = useState("");
  const [cameraActive, setCameraActive] = useState(false);
  const [outcome, setOutcome] = useState<CheckinOutcome>({ status: "idle" });
  const inputRef = useRef<HTMLInputElement>(null);
  const lastHandledCodeRef = useRef<string | null>(null);
  const inFlightCodeRef = useRef<string | null>(null);
  const checkedInRef = useRef(new Map<string, CheckinResponse>());
  const checkinMutation = useCheckin();
  const { playSuccess, playError } = useCheckinSound();

  const handleScan = useCallback(
    (qrCode: string, source: ScanSource) => {
      const trimmed = qrCode.trim();
      if (!trimmed || checkinMutation.isPending) return;

      if (trimmed === inFlightCodeRef.current) {
        setCode("");
        return;
      }
      if (source === "camera" && trimmed === lastHandledCodeRef.current) return;

      inFlightCodeRef.current = trimmed;
      lastHandledCodeRef.current = trimmed;
      checkinMutation.mutate(
        { qrCode: trimmed },
        {
          onSuccess: (res) => {
            checkedInRef.current.set(trimmed, res.data);
            setOutcome({ status: "success", result: res.data });
            playSuccess();
          },
          onError: (err: unknown) => {
            const failure = classifyCheckinError(err, checkedInRef.current.get(trimmed));
            if (failure.kind === "unknown") lastHandledCodeRef.current = null;
            setOutcome({ status: "error", failure });
            playError();
          },
          onSettled: () => {
            inFlightCodeRef.current = null;
          },
        },
      );

      setCode("");
      inputRef.current?.focus();
    },
    [checkinMutation, playError, playSuccess],
  );

  const handleCloseResult = useCallback(() => {
    lastHandledCodeRef.current = null;
    setOutcome({ status: "idle" });
    inputRef.current?.focus();
  }, []);

  const cardStatus = checkinMutation.isPending ? "pending" : outcome.status;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Soát vé & Check-in"
        description="Quét mã QR hoặc nhập mã vé để xác nhận khách vào rạp."
      />

      <CheckinResultCard
        status={cardStatus}
        result={outcome.status === "success" ? outcome.result : null}
        failure={outcome.status === "error" ? outcome.failure : null}
        onClose={handleCloseResult}
      />

      <div className="grid gap-6 md:grid-cols-2">
        <Card className="flex flex-col gap-4 p-5">
          <CheckinCodeInput
            ref={inputRef}
            value={code}
            onChange={setCode}
            onSubmit={() => handleScan(code, "manual")}
            disabled={checkinMutation.isPending}
          />

          <Button
            type="button"
            onClick={() => handleScan(code, "manual")}
            disabled={!code.trim() || checkinMutation.isPending}
          >
            <Ticket className="size-4" />
            <span>Soát vé ngay</span>
          </Button>
        </Card>

        <Card className="p-5">
          <CheckinCameraPanel
            active={cameraActive}
            onToggle={() => setCameraActive((prev) => !prev)}
            onDetect={(value) => handleScan(value, "camera")}
          />
        </Card>
      </div>
    </div>
  );
};

export default AdminCheckinPage;
