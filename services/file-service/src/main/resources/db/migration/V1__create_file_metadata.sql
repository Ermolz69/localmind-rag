create table file_metadata (
  id uuid primary key,
  original_name varchar(512) not null,
  sanitized_name varchar(512) not null,
  content_type varchar(255) not null,
  size_bytes bigint not null,
  storage_bucket varchar(255) not null,
  storage_object_key varchar(1024) not null unique,
  checksum_sha256 varchar(64) not null,
  status varchar(32) not null,
  created_at timestamptz not null,
  updated_at timestamptz not null
);

create index ix_file_metadata_status on file_metadata(status);
create index ix_file_metadata_created_at on file_metadata(created_at);

create table processing_jobs (
  id uuid primary key,
  file_id uuid not null references file_metadata(id),
  status varchar(32) not null,
  current_step varchar(64) not null,
  error_code varchar(128),
  error_message varchar(1024),
  created_at timestamptz not null,
  updated_at timestamptz not null
);

create index ix_processing_jobs_file_id on processing_jobs(file_id);
create index ix_processing_jobs_status on processing_jobs(status);
