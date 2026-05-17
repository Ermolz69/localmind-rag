export type BucketDto = {
  id: string;
  name: string;
  description: string | null;
  syncStatus: string | number;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateBucketRequest = {
  name: string;
  description?: string | null;
};
