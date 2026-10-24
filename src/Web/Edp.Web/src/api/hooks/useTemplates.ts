import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query';
import { placeholderApi, templateApi } from '../endpoints';
import type { PlaceholderInput, PlaceholderUpdate, TemplateFilters } from '../contracts';
import { useInvalidation, useQueryKeys } from '../queryKeys';

export function useTemplates(filters: TemplateFilters) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.templates.list(filters), queryFn: () => templateApi.list(filters), placeholderData: keepPreviousData });
}

export function useTemplate(id: string) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.templates.detail(id), queryFn: () => templateApi.get(id), enabled: !!id });
}

export function useTemplateVersions(id: string) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.templates.versions(id), queryFn: () => templateApi.versions(id), enabled: !!id });
}

export function usePlaceholders(templateId: string, versionId: string) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.templates.placeholders(templateId, versionId), queryFn: () => placeholderApi.list(templateId, versionId), enabled: !!templateId && !!versionId });
}

export function useValidationResult(templateId: string, versionId: string, enabled = true) {
  const keys = useQueryKeys();
  return useQuery({
    queryKey: keys.templates.validation(templateId, versionId),
    queryFn: () => templateApi.validation(templateId, versionId),
    enabled: enabled && !!templateId && !!versionId,
    retry: false
  });
}

export function useTemplateMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.templates();
  return {
    create: useMutation({ mutationFn: templateApi.create, onSuccess }),
    update: useMutation({ mutationFn: ({ id, ...body }: { id: string; name: string; description?: string; rowVersion: string }) => templateApi.update(id, body), onSuccess }),
    deactivate: useMutation({ mutationFn: (id: string) => templateApi.deactivate(id), onSuccess }),
    archive: useMutation({ mutationFn: (id: string) => templateApi.archive(id), onSuccess }),
    validate: useMutation({ mutationFn: ({ templateId, versionId }: { templateId: string; versionId: string }) => templateApi.validate(templateId, versionId), onSuccess }),
    activateVersion: useMutation({ mutationFn: ({ templateId, versionId }: { templateId: string; versionId: string }) => templateApi.activateVersion(templateId, versionId), onSuccess })
  };
}

interface Scope { templateId: string; versionId: string }

export function usePlaceholderMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.templates();
  return {
    create: useMutation({ mutationFn: ({ templateId, versionId, body }: Scope & { body: PlaceholderInput }) => placeholderApi.create(templateId, versionId, body), onSuccess }),
    update: useMutation({ mutationFn: ({ templateId, versionId, id, body }: Scope & { id: string; body: PlaceholderUpdate }) => placeholderApi.update(templateId, versionId, id, body), onSuccess }),
    remove: useMutation({ mutationFn: ({ templateId, versionId, id }: Scope & { id: string }) => placeholderApi.remove(templateId, versionId, id), onSuccess }),
    discover: useMutation({ mutationFn: ({ templateId, versionId }: Scope) => placeholderApi.discover(templateId, versionId), onSuccess }),
    validate: useMutation({ mutationFn: ({ templateId, versionId }: Scope) => placeholderApi.validate(templateId, versionId), onSuccess })
  };
}
