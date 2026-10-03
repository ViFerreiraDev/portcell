/// <reference types="vite/client" />

const baseUrl = import.meta.env.VITE_API_URL ?? '';

export async function downloadReport(path: string, token: string, orderNumber: number): Promise<void> {
  const response = await fetch(`${baseUrl}${path}`, { headers: { Authorization: `Bearer ${token}` }, credentials: 'include' });
  if (!response.ok) throw new Error(`Falha ao abrir o relatório (${response.status}).`);
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url; link.download = `PortCell-OS-${orderNumber}.pdf`;
  document.body.append(link); link.click(); link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60000);
}

export async function request<T>(path: string, token?: string, method = 'GET', body?: unknown): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method,
    credentials: 'include',
    headers: { ...(body === undefined ? {} : { 'Content-Type': 'application/json' }), ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (response.status === 401) throw new Error('Sessão expirada ou credenciais inválidas.');
  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Falha na requisição (${response.status}).`);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export type Cliente = { id: string; nome: string; telefone: string; email?: string };
export type Aparelho = { id: string; clienteId: string; marca: string; modelo: string; imei?: string };
export type Unidade = { id: string; nome: string };
export type ChecklistItem = { id: string; nome: string; ordem: number; ativo: boolean; opcoes: string[]; permiteObservacao: boolean };
export type Ordem = { id: string; numero: number; clienteId: string; aparelhoId: string; status: string; defeitoRelatado: string; acessoNecessario: boolean; atendimentoDireto: boolean; createdAt: string };
export type TipoReparo = { id: string; nome: string; requerDesbloqueio: boolean; ativo: boolean };
export type Orcamento = { id: string; numeroVersao: number; total: number; validoAte: string };
export type Lista<T> = { itens: T[]; total: number; pagina: number };
