import { AlertTriangle, QrCode, Search } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { CheckinCameraPanel } from "@/features/admin/checkin/components/checkin-camera-panel";

import type { EventGiftHandoverFlow } from "../hooks/use-event-gift-handover-flow";

export const EventGiftLookupPanel = ({ flow }: { flow: EventGiftHandoverFlow }) => {
  const submitCode = () => flow.findTicket(flow.code, "manual");

  return (
    <Card className="flex flex-col gap-4 p-5">
      <div className="relative">
        <QrCode className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          autoFocus
          disabled={flow.isLooking || flow.isSubmitting}
          value={flow.code}
          onChange={(event) => flow.setCode(event.target.value)}
          onKeyDown={(event) => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            submitCode();
          }}
          placeholder="Quét QR vé hoặc nhập mã vé"
          aria-label="Mã QR của vé"
          className="h-12 pl-9 text-base tabular-nums"
        />
      </div>

      <Button
        type="button"
        onClick={submitCode}
        disabled={!flow.code.trim() || flow.isLooking || flow.isSubmitting}
      >
        <Search className="size-4" />
        <span>{flow.isLooking ? FORM_PROCESSING_LABEL : "Tìm vé"}</span>
      </Button>

      {flow.lookupError && (
        <div
          role="alert"
          className="flex items-start gap-2 bg-destructive/10 px-3 py-2 text-sm text-destructive"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span className="min-w-0 wrap-break-word">{flow.lookupError}</span>
        </div>
      )}

      <CheckinCameraPanel
        active={flow.cameraActive}
        onToggle={flow.toggleCamera}
        onDetect={(value) => flow.findTicket(value, "camera")}
      />
    </Card>
  );
};
