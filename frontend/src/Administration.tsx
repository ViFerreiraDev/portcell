import { useEffect, useState } from 'react';
import { Alert, Button, Checkbox, FormControlLabel, MenuItem, Paper, Stack, Tab, Tabs, TextField, Typography } from '@mui/material';
import { request, type ChecklistItem, type TipoReparo } from './api';
import { ServiceAdministration } from './ServiceAdministration';
import { ChecklistItemEditor } from './ChecklistItemEditor';
import { UserControls, type ManagedUser } from './UserControls';
import { SectionHeader } from './ui';

type TestItem = { id: string; nome: string; ordem: number; ativo: boolean };

export function Administration({ token }: { token: string }) {
  const [users, setUsers] = useState<ManagedUser[]>([]); const [checklist, setChecklist] = useState<ChecklistItem[]>([]); const [types, setTypes] = useState<TipoReparo[]>([]);
  const [tests, setTests] = useState<TestItem[]>([]); const [testName, setTestName] = useState('');
  const [name, setName] = useState(''); const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [role, setRole] = useState('Atendente');
  const [checkName, setCheckName] = useState(''); const [checkOptions, setCheckOptions] = useState('OK, Avariado, Não testado, Não aplicável'); const [typeName, setTypeName] = useState(''); const [unlock, setUnlock] = useState(false);
  const [error, setError] = useState('');
  const [section, setSection] = useState(0);
  async function load() {
    try { const [u, c, t, testItems] = await Promise.all([request<ManagedUser[]>('/api/usuarios', token), request<ChecklistItem[]>('/api/configuracoes/checklist', token), request<TipoReparo[]>('/api/tipos-reparo', token), request<TestItem[]>('/api/configuracoes/testes', token)]);
      setUsers(u); setChecklist(c); setTypes(t); setTests(testItems); setError('');
    } catch (error) { setError(String(error)); }
  }
  useEffect(() => { void load(); }, [token]);
  async function act(path: string, body: unknown, method = 'POST') { try { await request(path, token, method, body); await load(); } catch (error) { setError(String(error)); } }
  return <Stack spacing={2}><SectionHeader eyebrow="Configurações" title="Administração" description="Gerencie usuários, checklists e parâmetros da assistência." />{error && <Alert severity="error">{error}</Alert>}
    <Paper sx={{ px: 1.5, overflow: 'hidden' }}><Tabs value={section} onChange={(_, value: number) => setSection(value)} variant="scrollable" scrollButtons="auto" aria-label="Seções da administração"><Tab label="Usuários"/><Tab label="Checklist"/><Tab label="Reparos"/><Tab label="Testes finais"/><Tab label="Serviços"/></Tabs></Paper>
    {section === 0 && <Paper sx={{ p: 3 }}><Stack spacing={2}><Typography variant="h6">Usuários</Typography>
      <Stack component="form" autoComplete="off" spacing={2} onSubmit={event => { event.preventDefault(); void act('/api/usuarios', { nome: name, email, senha: password, perfil: role }); setName(''); setEmail(''); setPassword(''); }}><TextField label="Nome" name="newUserName" autoComplete="off" required value={name} onChange={event => setName(event.target.value)} /><TextField label="E-mail" name="newUserEmail" autoComplete="off" type="email" required value={email} onChange={event => setEmail(event.target.value)} /><TextField label="Senha inicial (mínimo 12 caracteres)" name="newUserPassword" autoComplete="new-password" type="password" required value={password} onChange={event => setPassword(event.target.value)} /><TextField select label="Perfil" value={role} onChange={event => setRole(event.target.value)}>{['Administrador', 'Gerente', 'Atendente', 'Tecnico'].map(x => <MenuItem key={x} value={x}>{x}</MenuItem>)}</TextField><Button type="submit" variant="contained">Criar usuário</Button></Stack>
      {users.map(x => <UserControls key={x.id} user={x} token={token} onChanged={load} />)}
    </Stack></Paper>}
    {section === 1 && <Paper sx={{ p: 3 }}><Stack spacing={2}><Typography variant="h6">Checklist de entrada</Typography>
      <Stack component="form" spacing={1} onSubmit={event => { event.preventDefault(); void act('/api/configuracoes/checklist', { nome: checkName, ordem: checklist.length + 1, ativo: true, opcoes: checkOptions.split(',').map(x => x.trim()).filter(Boolean) }); setCheckName(''); }}>
        <TextField label="Novo item" required value={checkName} onChange={event => setCheckName(event.target.value)} /><TextField label="Opções separadas por vírgula" required value={checkOptions} onChange={event => setCheckOptions(event.target.value)} /><Button type="submit">Adicionar item</Button>
      </Stack>
      {checklist.map(x => <ChecklistItemEditor key={x.id} item={x} onSave={async value => { await request(`/api/configuracoes/checklist/${x.id}`, token, 'PUT', value); await load(); }} />)}
    </Stack></Paper>}
    {section === 2 && <Paper component="form" sx={{ p: 3 }} onSubmit={event => { event.preventDefault(); void act('/api/tipos-reparo', { nome: typeName, requerDesbloqueio: unlock, ativo: true }); setTypeName(''); setUnlock(false); }}><Stack spacing={2}><Typography variant="h6">Tipos de reparo</Typography><TextField label="Novo tipo" required value={typeName} onChange={event => setTypeName(event.target.value)} /><FormControlLabel control={<Checkbox checked={unlock} onChange={event => setUnlock(event.target.checked)} />} label="Requer desbloqueio" /><Button type="submit" variant="contained">Adicionar tipo</Button>{types.map(x => <Typography key={x.id}>{x.nome} · {x.requerDesbloqueio ? 'Requer desbloqueio' : 'Sem acesso ao dispositivo'} · {x.ativo ? 'Ativo' : 'Inativo'}</Typography>)}</Stack></Paper>}
    {section === 3 && <Paper component="form" sx={{ p: 3 }} onSubmit={event => { event.preventDefault(); void act('/api/configuracoes/testes', { nome: testName, ordem: tests.length + 1, ativo: true }); setTestName(''); }}><Stack spacing={2}><Typography variant="h6">Testes finais</Typography><TextField label="Novo teste" required value={testName} onChange={event => setTestName(event.target.value)} /><Button type="submit" variant="contained">Adicionar teste</Button>{tests.map(x => <Stack key={x.id} direction="row" spacing={1} alignItems="center"><Typography sx={{ flexGrow: 1 }}>{x.ordem}. {x.nome}</Typography><Button size="small" onClick={() => act(`/api/configuracoes/testes/${x.id}`, { nome: x.nome, ordem: x.ordem, ativo: !x.ativo }, 'PUT')}>{x.ativo ? 'Desativar' : 'Ativar'}</Button></Stack>)}</Stack></Paper>}
    {section === 4 && <ServiceAdministration token={token} />}
  </Stack>;
}
