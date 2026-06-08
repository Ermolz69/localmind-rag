create table folders (
  id uuid primary key,
  owner_user_id varchar(128) not null,
  parent_folder_id uuid references folders(id),
  name varchar(255) not null,
  path varchar(1024) not null,
  created_at timestamptz not null,
  updated_at timestamptz not null
);

create index ix_folders_owner_user_id on folders(owner_user_id);
create index ix_folders_parent_id on folders(parent_folder_id);

create table file_metadata (
  id uuid primary key,
  owner_user_id varchar(128) not null,
  folder_id uuid references folders(id),
  original_name varchar(512) not null,
  storage_object_key varchar(1024) not null unique,
  size_bytes bigint not null,
  content_type varchar(255) not null,
  created_at timestamptz not null
);

create index ix_file_metadata_owner_user_id on file_metadata(owner_user_id);
create index ix_file_metadata_folder_id on file_metadata(folder_id);
create index ix_file_metadata_created_at on file_metadata(created_at);
