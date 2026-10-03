import { useEffect, useState } from 'react';
import { Alert, Box, Button, InputAdornment, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { downloadReport, request, type Aparelho, type Cliente, type Orcamento, type Ordem, type TipoReparo, type Unidade } from './api';
import { OrderActions } from './OrderActions';
import { QuoteComposer } from './QuoteComposer';
import { ManualAuthorization } from './ManualAuthorization';
import { EmptyState, Icon, SectionHeader, StatusChip } from './ui';
import { InitialIntake, type InitialResult } from './InitialIntake';

export type OrderIntent = { type: 'list' | 'new' | 'detail'; order?: Ordem };
export function OrdersPage({ token, profile, orders, clients, devices, units, repairTypes, intent, onRefresh }: {
  token: string; profile: string; orders: Ordem[]; clients: Cliente[]; devices: Aparelho[]; units: Unidade[]; repairTypes: TipoReparo[];
  intent: OrderIntent; onRefresh: () => Promise<void>;
}) {
  const [view, setView] = useState<'list' | 'new' | 'detail'>(intent.type);
  const [selected, setSelected] = useState<Ordem | null>(intent.order ?? null);
  const [versions, setVersions] = useState<Orcamento[]>([]);
  const [approvalLink, setApprovalLink] = useState(''); const [error, setError] = useState('');
  const [emailInfo, setEmailInfo] = useState<{ status: string; mensagem: string } | null>(null);
  const [sendingEmail, setSendingEmail] = useState(false);
  const [search, setSearch] = useState(''); const [statusFilter, setStatusFilter] = useState('');
  const [diagnosis, setDiagnosis] = useState(''); const [clientSummary, setClientSummary] = useState('');
  useEffect(() => { setView(intent.type); if (intent.order) void openOrder(intent.order); }, [intent]);
  const filtered = orders.filter(x => {
    const client = clients.find(c => c.id === x.clienteId); const device = devices.find(d => d.id === x.aparelhoId);
    return (!statusFilter || x.status === statusFilter) && `${x.numero} ${x.defeitoRelatado} ${client?.nome ?? ''} ${device?.marca ?? ''} ${device?.modelo ?? ''}`.toLocaleLowerCase('pt-BR').includes(search.toLocaleLowerCase('pt-BR'));
  });
  async function openOrder(order: Ordem) { setSelected(order); setView('detail'); setApprovalLink(''); setEmailInfo(null); try { setVersions(await request<Orcamento[]>(`/api/ordens-servico/${order.id}/orcamentos`, token)); setError(''); } catch (error) { setError(String(error)); } }
  async function act(action: () => Promise<unknown>, after?: () => void) { try { await action(); after?.(); await onRefresh(); setError(''); } catch (error) { setError(String(error)); } }
  async function changeStatus(status: string) { if (selected) await act(() => request(`/api/ordens-servico/${selected.id}/status`, token, 'PATCH', { status }), () => setSelected({ ...selected, status })); }
  function handleInitialCreated(result: InitialResult) { setSelected(result.ordem); setVersions([result.orcamento]); setApprovalLink(result.linkAprovacao); setEmailInfo(result.email); setView('detail'); }
  async function resendEmail() {
    if (!selected) return;
    setSendingEmail(true);
    try { const result = await request<{ email: { status: string; mensagem: string }; linkAprovacao?: string }>(`/api/ordens-servico/${selected.id}/enviar-email`, token, 'POST'); setEmailInfo(result.email); if (result.linkAprovacao) setApprovalLink(result.linkAprovacao); setError(''); }
    catch (error) { setError(String(error)); }
    finally { setSendingEmail(false); }
  }
  const back = () => { setView('list'); setSelected(null); setApprovalLink(''); setError(''); };
  return <>
    <SectionHeader eyebrow="Atendimento e reparo" title={view === 'new' ? 'Nova ordem de serviço' : view === 'detail' && selected ? `OS #${selected.numero}` : 'Ordens de serviço'} description={view === 'new' ? 'Cadastre, inspecione e apresente o orçamento em um só atendimento.' : view === 'detail' ? 'Acompanhe as etapas, o orçamento e as ações desta OS.' : 'Acompanhe cada atendimento, do recebimento à entrega.'} action={view === 'list' ? <Button variant="contained" startIcon={<Icon name="plus" size={17}/>} onClick={() => setView('new')}>Abrir nova OS</Button> : <Button variant="outlined" onClick={back}>Voltar para a lista</Button>} />
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    {view === 'list' && <>
      <Paper sx={{ p: 2.5, mb: 2.5 }}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}><Box sx={{ flex: 1 }}><Typography variant="h6">Fila de atendimento</Typography><Typography color="text.secondary" sx={{ fontSize: 12.5 }}>{orders.length} {orders.length === 1 ? 'ordem carregada' : 'ordens carregadas'}</Typography></Box><TextField size="small" placeholder="Buscar OS, cliente ou aparelho" value={search} onChange={e => setSearch(e.target.value)} sx={{ width: { xs: '100%', sm: 270 } }} InputProps={{ startAdornment: <InputAdornment position="start"><Icon name="search" size={17}/></InputAdornment> }}/><TextField select size="small" label="Status" value={statusFilter} onChange={e => setStatusFilter(e.target.value)} sx={{ minWidth: { xs: '100%', sm: 180 } }}><MenuItem value="">Todos</MenuItem>{Array.from(new Set(orders.map(x => x.status))).map(x => <MenuItem key={x} value={x}>{x.replace(/([a-z])([A-Z])/g, '$1 $2')}</MenuItem>)}</TextField></Stack></Paper>
      {filtered.length === 0 ? <EmptyState icon="orders" title={search || statusFilter ? 'Nenhuma OS encontrada' : 'Sua fila começa aqui'} description={search || statusFilter ? 'Tente outro termo ou filtro de status.' : 'Abra uma ordem de serviço para acompanhar cada etapa do atendimento.'} action={search || statusFilter ? undefined : { label: 'Abrir OS', onClick: () => setView('new') }} /> : <Paper sx={{ overflow: 'hidden' }}>{filtered.map((x, index) => {
        const client = clients.find(c => c.id === x.clienteId); const device = devices.find(d => d.id === x.aparelhoId);
        return <Box component="button" key={x.id} onClick={() => { void openOrder(x); }} sx={{ width: '100%', display: 'flex', gap: 2, alignItems: 'center', p: { xs: 2, sm: 2.5 }, textAlign: 'left', border: 0, borderBottom: index < filtered.length - 1 ? '1px solid #edf0f1' : 0, bgcolor: '#fff', cursor: 'pointer', '&:hover': { bgcolor: '#f8fbfb' } }}><Box sx={{ width: 43, height: 43, flexShrink: 0, borderRadius: 2, bgcolor: '#eaf4f3', color: 'primary.main', display: 'grid', placeItems: 'center' }}><Icon name="devices" size={20}/></Box><Box sx={{ minWidth: 0, flex: 1 }}><Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'center' }} spacing={1}><Typography sx={{ fontWeight: 800, fontSize: 14 }}>OS #{x.numero}</Typography><Typography color="text.secondary" sx={{ fontSize: 13 }}>{device ? `${device.marca} ${device.modelo}` : 'Aparelho'}</Typography></Stack><Typography noWrap color="text.secondary" sx={{ fontSize: 12.5, mt: .35 }}>{client?.nome ?? 'Cliente'} · {x.defeitoRelatado}</Typography></Box><Box sx={{ display: { xs: 'none', sm: 'block' } }}><StatusChip status={x.status}/></Box><Box sx={{ color: '#a4b4bb' }}><Icon name="arrow" size={17}/></Box></Box>;
      })}</Paper>}
    </>}
    {view === 'new' && <InitialIntake token={token} units={units} clients={clients} devices={devices} repairTypes={repairTypes} onRefresh={onRefresh} onCreated={handleInitialCreated} />}
    {view === 'detail' && selected && <Stack spacing={3}>
      <Paper sx={{ p: { xs: 2.5, sm: 3 } }}><Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}><Box><Typography sx={{ color: 'text.secondary', fontSize: 12, fontWeight: 750, mb: .7 }}>ORDEM DE SERVIÇO #{selected.numero}</Typography><Typography variant="h5">{devices.find(x => x.id === selected.aparelhoId)?.marca} {devices.find(x => x.id === selected.aparelhoId)?.modelo}</Typography><Typography color="text.secondary" sx={{ mt: .7 }}>{clients.find(x => x.id === selected.clienteId)?.nome ?? 'Cliente'}</Typography></Box><Box><StatusChip status={selected.status}/></Box></Stack><Box sx={{ bgcolor: '#f7f9f9', borderRadius: 2, p: 2, mt: 2.5 }}><Typography color="text.secondary" sx={{ fontSize: 11, fontWeight: 800, letterSpacing: '.08em' }}>DEFEITO RELATADO</Typography><Typography sx={{ mt: .6 }}>{selected.defeitoRelatado}</Typography></Box></Paper>
      {selected.atendimentoDireto && <Paper sx={{ p: 3 }}><Typography variant="h6">Relatório de entrada e orçamento</Typography><Typography color="text.secondary" sx={{ fontSize: 13, mt: .5, mb: 2 }}>PDF A4 de uma página com todas as faces do aparelho, pontos constatados e espaço para assinatura.</Typography><Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}><Button variant="contained" onClick={() => { void downloadReport(`/api/ordens-servico/${selected.id}/relatorio.pdf`, token, selected.numero).catch(error => setError(String(error))); }}>Baixar relatório para assinatura</Button><Button variant="outlined" disabled={sendingEmail} onClick={() => { void resendEmail(); }}>{sendingEmail ? 'Enviando...' : 'Enviar cópia por e-mail'}</Button></Stack>{emailInfo && <Alert severity={emailInfo.status === 'enviado' ? 'success' : 'warning'} sx={{ mt: 2 }}>{emailInfo.mensagem}</Alert>}</Paper>}
      {(selected.status === 'Recebido' || selected.status === 'AguardandoDiagnostico' || selected.status === 'EmDiagnostico') && <Paper sx={{ p: 3 }}><Typography variant="h6" sx={{ mb: 2 }}>Próxima etapa</Typography>{selected.status === 'Recebido' && <Button variant="contained" onClick={() => { void changeStatus('AguardandoDiagnostico'); }}>Enviar para diagnóstico</Button>}{selected.status === 'AguardandoDiagnostico' && <Button variant="contained" onClick={() => { void changeStatus('EmDiagnostico'); }}>Iniciar diagnóstico</Button>}{selected.status === 'EmDiagnostico' && <Box component="form" onSubmit={event => { event.preventDefault(); void act(() => request(`/api/ordens-servico/${selected.id}/diagnosticos`, token, 'POST', { descricao: diagnosis, resumoCliente: clientSummary }), () => { setSelected({ ...selected, status: 'AguardandoOrcamento' }); setDiagnosis(''); setClientSummary(''); }); }}><Stack spacing={2}><TextField label="Diagnóstico técnico interno" multiline minRows={2} required value={diagnosis} onChange={e => setDiagnosis(e.target.value)} /><TextField label="Resumo para o cliente" multiline minRows={2} required value={clientSummary} onChange={e => setClientSummary(e.target.value)} /><Box><Button type="submit" variant="contained">Concluir diagnóstico</Button></Box></Stack></Box>}</Paper>}
      {(selected.status === 'AguardandoOrcamento' || selected.status === 'AguardandoAprovacao' || selected.status === 'Aprovado' || selected.status === 'OrcamentoRecusado') && <QuoteComposer orderId={selected.id} token={token} onCreated={quote => { setVersions([quote, ...versions]); setSelected({ ...selected, status: 'AguardandoAprovacao' }); void onRefresh(); }} />}
      {versions.length > 0 && <Paper sx={{ p: 3 }}><Typography variant="h6" sx={{ mb: 2 }}>Orçamentos</Typography><Stack spacing={1.5}>{versions.map(x => <Stack key={x.id} direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'center' }} spacing={1.5}><Typography sx={{ flex: 1, fontWeight: 650 }}>Versão {x.numeroVersao} · {x.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</Typography>{selected.status === 'AguardandoAprovacao' && x.id === versions[0]?.id && <Button variant="outlined" onClick={() => { void act(async () => { const result = await request<{ link: string }>(`/api/orcamentos/${x.id}/enviar-aprovacao`, token, 'POST'); setApprovalLink(result.link); }); }}>Gerar link de aprovação</Button>}</Stack>)}</Stack></Paper>}
      {selected.status === 'AguardandoAprovacao' && versions[0] && <ManualAuthorization orderId={selected.id} quote={versions[0]} token={token} onDecided={approved => { setSelected({ ...selected, status: approved ? 'Aprovado' : 'OrcamentoRecusado' }); void onRefresh(); }} />}
      {approvalLink && <Paper sx={{ p: 3 }}><TextField label="Link para enviar ao cliente" value={approvalLink} InputProps={{ readOnly: true }} fullWidth onFocus={e => e.target.select()} /></Paper>}
      <OrderActions order={selected} token={token} profile={profile} latest={versions[0]} onStatus={status => { setSelected({ ...selected, status }); void onRefresh(); }} />
    </Stack>}
  </>;
}
