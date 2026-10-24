export type TemplateStatus = 'Draft' | 'Active' | 'Inactive' | 'Archived';

export interface Template {
  id: string;
  name: string;
  code: string;
  description?: string;
  status: TemplateStatus | string;
  currentVersionId?: string;
  createdAt: string;
  createdBy?: string;
  modifiedAt?: string;
  modifiedBy?: string;
  rowVersion: string;
}

export const PLACEHOLDER_DATA_TYPES = ['String', 'Integer', 'Decimal', 'Boolean', 'Date', 'DateTime', 'Currency', 'Email', 'Phone', 'Url'] as const;
export type PlaceholderDataType = (typeof PLACEHOLDER_DATA_TYPES)[number];

export interface Placeholder {
  id: string;
  name: string;
  displayName?: string;
  dataType: PlaceholderDataType | string;
  isRequired: boolean;
  defaultValue?: string;
  format?: string;
  description?: string;
  occurrences: number;
}

export type PlaceholderInput = Omit<Placeholder, 'id'>;
export type PlaceholderUpdate = Partial<Omit<Placeholder, 'id' | 'name' | 'occurrences'>>;

export interface TemplateVersion {
  id: string;
  templateId: string;
  versionNumber: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  validationStatus: 'NotValidated' | 'Valid' | 'Invalid' | string;
  status: 'Uploaded' | 'Active' | 'Inactive' | string;
  changeDescription?: string;
  createdAt: string;
  createdBy?: string;
  placeholders: Placeholder[];
}

export interface ValidationIssue {
  code: string;
  severity: string;
  message: string;
  location?: string;
}

export interface ValidationResult {
  isValid: boolean;
  status: string;
  errorCount: number;
  warningCount: number;
  errors: ValidationIssue[];
  warnings: ValidationIssue[];
  validatedAt: string;
}

export interface PlaceholderDiscoveryResult {
  templateVersionId: string;
  discovered: string[];
  newPlaceholders: string[];
  existingPlaceholders: string[];
  missingFromDocument: string[];
  status: string;
}

export interface TemplateFilters {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}
