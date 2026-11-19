const GSI_SRC = "https://accounts.google.com/gsi/client";

let pending: Promise<void> | null = null;

export function loadGoogleIdentity(): Promise<void> {
  if (typeof window === "undefined") {
    return Promise.reject(new Error("Google Identity cần môi trường trình duyệt"));
  }
  if (window.google?.accounts?.id) return Promise.resolve();
  if (pending) return pending;

  pending = new Promise<void>((resolve, reject) => {
    const script = document.createElement("script");
    script.src = GSI_SRC;
    script.async = true;
    script.defer = true;
    script.onload = () => resolve();
    script.onerror = () => {
      script.remove();
      pending = null; // cho phép thử lại ở lần mount sau
      reject(new Error("Không tải được Google Identity Services"));
    };
    document.head.appendChild(script);
  });

  return pending;
}
