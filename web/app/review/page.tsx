import { Suspense } from "react";
import { ReviewView } from "@/components/review/review-view";

export default function ReviewPage() {
  return (
    <Suspense fallback={null}>
      <ReviewView />
    </Suspense>
  );
}
