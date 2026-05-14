import type { BucketDto } from "../../shared/api/client";

type BucketSelectorProps = {
  buckets: BucketDto[];
  value: string | null;
  onChange: (value: string | null) => void;
  label?: string;
};

export function BucketSelector({
  buckets,
  value,
  onChange,
  label = "Bucket",
}: BucketSelectorProps) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium">{label}</label>
      <select
        className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm text-foreground"
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value || null)}
      >
        <option value="">No bucket</option>
        {buckets.map((bucket) => (
          <option key={bucket.id} value={bucket.id}>
            {bucket.name}
          </option>
        ))}
      </select>
    </div>
  );
}
