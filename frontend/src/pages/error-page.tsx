import { ErrorState } from "@/components/common/error-state";

const ErrorPage = () => <ErrorState variant="page" onRetry={() => window.location.reload()} />;

export default ErrorPage;
