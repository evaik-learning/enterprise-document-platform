import { httpClient, uploadWithProgress } from './httpClient';

/** Centralized Gateway client: components never issue raw HTTP calls (see Phase 10 section 37). */
export const gatewayClient = {
  get: httpClient.get,
  post: httpClient.post,
  put: httpClient.put,
  patch: httpClient.patch,
  delete: httpClient.delete,
  download: httpClient.download,
  upload: uploadWithProgress
};
