import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Alert, CssBaseline, ThemeProvider } from '@mui/material';
import { request, type Aparelho, type ChecklistItem, type Cliente, type Lista, type Ordem, type TipoReparo, type Unidade } from './api';
import { PublicApproval } from './PublicApproval';
import { Inventory } from './Inventory';
import { Administration } from './Administration';
import { Dashboard } from './Dashboard';
import { Reports } from './Reports';
import { LoginScreen, Shell, type Page } from './Shell';
import { CustomersPage, DevicesPage } from './CustomersDevices';
import { OrdersPage, type OrderIntent } from './OrdersPage';
import { theme } from './theme';

const approvalToken = location.pathname.match(/^\/aprovar\/([^/]+)$/)?.[1];
type AuthResult = { token: string; perfil: string; nome: string };

function App() {
  const [token, setToken] = useState(() => sessionStorage.getItem('portcell_token') ?? '');
  const [profile, setProfile] = useState(() => sessionStorage.getItem('portcell_profile') ?? '');
  const [name, setName] = useState(() => sessionStorage.getItem('portcell_name') ?? 'Equipe PortCell');
  const [loginEmail, setLoginEmail] = useState('');
  const [loginPassword, setLoginPassword] = useState('');
  const [error, setError] = useState('');
  const [page, setPage] = useState<Page>('dashboard');
  const [orderIntent, setOrderIntent] = useState<OrderIntent>({ type: 'list' });
  const [clients, setClients] = useState<Cliente[]>([]);
  const [clientTotal, setClientTotal] = useState(0);
  const [devices, setDevices] = useState<Aparelho[]>([]);
  const [units, setUnits] = useState<Unidade[]>([]);
  const [checklist, setChecklist] = useState<ChecklistItem[]>([]);
  const [repairTypes, setRepairTypes] = useState<TipoReparo[]>([]);
  const [orders, setOrders] = useState<Ordem[]>([]);

  function keepSession(result: AuthResult) {
    sessionStorage.setItem('portcell_token', result.token);
    sessionStorage.setItem('portcell_profile', result.perfil);
    sessionStorage.setItem('portcell_name', result.nome);
    setToken(result.token); setProfile(result.perfil); setName(result.nome);
  }
  useEffect(() => {
    if (token) return;
    request<AuthResult>('/api/auth/refresh', undefined, 'POST').then(keepSession).catch(() => {});
  }, []);

  async function load() {
    if (!token) return;
    try {
      const [clientPage, deviceList, unitList, checklistList, orderPage, types] = await Promise.all([
        request<Lista<Cliente>>('/api/clientes', token), request<Aparelho[]>('/api/aparelhos', token),
        request<Unidade[]>('/api/unidades', token), request<ChecklistItem[]>('/api/configuracoes/checklist', token),
        request<Lista<Ordem>>('/api/ordens-servico', token), request<TipoReparo[]>('/api/tipos-reparo', token),
      ]);
      setClients(clientPage.itens); setClientTotal(clientPage.total); setDevices(deviceList); setUnits(unitList); setChecklist(checklistList.filter(x => x.ativo)); setOrders(orderPage.itens); setRepairTypes(types.filter(x => x.ativo)); setError('');
    } catch (error) {
      if (String(error).includes('Sessão expirada')) {
        try { keepSession(await request<AuthResult>('/api/auth/refresh', undefined, 'POST')); }
        catch { sessionStorage.removeItem('portcell_token'); setToken(''); setError('Sessão encerrada. Entre novamente.'); }
      } else setError(String(error));
    }
  }
  useEffect(() => { void load(); }, [token]);
  async function login(event: React.FormEvent) {
    event.preventDefault();
    try { keepSession(await request<AuthResult>('/api/auth/login', undefined, 'POST', { email: loginEmail, senha: loginPassword })); setLoginPassword(''); setError(''); }
    catch (error) { setError(String(error)); }
  }
  function logout() {
    void request('/api/auth/logout', undefined, 'POST').catch(() => {});
    ['portcell_token', 'portcell_profile', 'portcell_name'].forEach(key => sessionStorage.removeItem(key));
    setToken(''); setProfile(''); setName('Equipe PortCell'); setPage('dashboard');
  }
  function navigate(next: Page) { if (next === 'ordens') setOrderIntent({ type: 'list' }); setPage(next); }
  function showOrder(intent: OrderIntent) { setOrderIntent(intent); setPage('ordens'); }

  if (approvalToken) return <PublicApproval token={approvalToken} />;
  if (!token) return <LoginScreen email={loginEmail} password={loginPassword} error={error} onEmail={setLoginEmail} onPassword={setLoginPassword} onSubmit={login} />;
  return <Shell page={page} setPage={navigate} profile={profile} name={name} onLogout={logout}>
    {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}
    {page === 'dashboard' && <Dashboard token={token} orders={orders} clients={clients} clientTotal={clientTotal} devices={devices} onOrders={() => showOrder({ type: 'list' })} onNewOrder={() => showOrder({ type: 'new' })} onSelectOrder={order => showOrder({ type: 'detail', order })} />}
    {page === 'clientes' && <CustomersPage token={token} clients={clients} onRefresh={load} />}
    {page === 'aparelhos' && <DevicesPage token={token} devices={devices} clients={clients} onRefresh={load} />}
    {page === 'ordens' && <OrdersPage token={token} profile={profile} orders={orders} clients={clients} devices={devices} units={units} repairTypes={repairTypes} intent={orderIntent} onRefresh={load} />}
    {page === 'estoque' && <Inventory token={token} profile={profile} />}
    {page === 'relatorios' && ['Administrador', 'Gerente'].includes(profile) && <Reports token={token} />}
    {page === 'admin' && profile === 'Administrador' && <Administration token={token} />}
  </Shell>;
}

createRoot(document.getElementById('root')!).render(<React.StrictMode><ThemeProvider theme={theme}><CssBaseline/><App /></ThemeProvider></React.StrictMode>);
