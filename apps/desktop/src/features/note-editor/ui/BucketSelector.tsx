import type { BucketDto } from "@entities/bucket";
import { Select } from "@shared/ui";

type BucketSelectorProps = {
  buckets: BucketDto[];
  value: string | null;
  onChange: (bucketId: string | null) => void;
};

export function BucketSelector({
  buckets,
  value,
  onChange,
}: BucketSelectorProps) {
  return (
    <label className="flex flex-col gap-2 text-sm">
      <span className="font-medium">Bucket</span>
      <Select
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value || null)}
      >
        <option value="">Default bucket</option>
        {buckets.map((bucket) => (
          <option key={bucket.id} value={bucket.id}>
            {bucket.name}
          </option>
        ))}
      </Select>
    </label>
  );
}
