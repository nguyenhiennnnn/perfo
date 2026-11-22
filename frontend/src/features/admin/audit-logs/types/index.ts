export interface AuditLogListParams {
  entityType?: string;
  entityId?: string;
  actorUserId?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface AuditLogListItemDto {
  auditLogId: number;
  occurredAt: string;
  actorUserId?: number | null;
  actorName?: string | null;
  action: string;
  entityType: string;
  entityId: string;
}

export interface AuditLogDetailDto extends AuditLogListItemDto {
  actorEmail?: string | null;
  beforeJson?: string | null;
  afterJson?: string | null;
}

export interface AuditLogActorDto {
  userId: number;
  fullName: string;
}

export interface AuditLogFilterOptionsDto {
  entityTypes: string[];
  actors: AuditLogActorDto[];
}
