import type { components, operations } from "./generated";

type OperationName = keyof operations;
type SchemaName = keyof components["schemas"];
type SuccessStatus = 200 | 201 | 202 | 204;

type IsUnknown<T> = unknown extends T
  ? [keyof T] extends [never]
    ? true
    : false
  : false;

export type NormalizeApi<T> =
  IsUnknown<T> extends true
    ? T
    : [number] extends [T]
      ? [string] extends [T]
        ? NormalizeApi<Exclude<T, number | string>> | number
        : T
      : T extends readonly (infer Item)[]
        ? NormalizeApi<Item>[]
        : T extends object
          ? { [Key in keyof T]: NormalizeApi<T[Key]> }
          : T;

export type Schema<Name extends SchemaName> = NormalizeApi<
  components["schemas"][Name]
>;

type OperationParameter<
  Name extends OperationName,
  Parameter extends "query" | "path",
> = Parameter extends keyof operations[Name]["parameters"]
  ? NormalizeApi<NonNullable<operations[Name]["parameters"][Parameter]>>
  : never;

export type OperationQuery<Name extends OperationName> = OperationParameter<
  Name,
  "query"
>;

export type OperationPath<Name extends OperationName> = OperationParameter<
  Name,
  "path"
>;

type OperationRequestContent<Name extends OperationName> =
  operations[Name] extends {
    requestBody: { content: infer Content };
  }
    ? Content
    : never;

export type OperationJsonBody<Name extends OperationName> =
  OperationRequestContent<Name> extends {
    "application/json": infer Body;
  }
    ? NormalizeApi<Body>
    : never;

type OperationSuccessResponse<Name extends OperationName> =
  operations[Name] extends { responses: infer Responses }
    ? Responses[Extract<keyof Responses, SuccessStatus>]
    : never;

type JsonResponse<Response> = Response extends {
  content: { "application/json": infer Json };
}
  ? Json
  : never;

type UnwrapResponseData<Response> = Response extends { data: infer Data }
  ? IsUnknown<Data> extends true
    ? void
    : NormalizeApi<Exclude<Data, null | undefined>>
  : NormalizeApi<Response>;

export type OperationData<Name extends OperationName> = UnwrapResponseData<
  JsonResponse<OperationSuccessResponse<Name>>
>;

export type ApiErrorDetail = Schema<"ApiErrorDetail">;
export type ApiEnvelopeError = Schema<"ApiError">;
export type ApiMetadata = Schema<"ApiMetadata">;

type ApiResponseShape = Schema<"ApiResponseOfObject">;

export type ApiResponse<T> = Omit<ApiResponseShape, "data"> & {
  data: T | null;
};

type CursorPageShape = Schema<"CursorPageOfBucketDto">;

export type CursorPage<T> = Omit<CursorPageShape, "items"> & {
  items: T[];
};

export type CursorPageRequest = Pick<
  OperationQuery<"ListChats">,
  "cursor" | "limit"
>;

export type IngestionJobDto = Schema<"IngestionJobDto">;
