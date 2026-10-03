import { useEffect, useState, type ReactNode } from 'react';
import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { request } from './api';
import { EmptyState, SectionHeader, StatusChip } from './ui';

type Report = {
  inicio: string; fim: string;
  porStatus: { status: string; quantidade: number }[];
  porTecnico: { tecnicoId: string; nome: string; quantidade: number }[];
  retornosGarantia: number;
  pagamentosPorForma: { forma: string; valor: number; movimentos: number }[];
  faturamento: number; ticketMedio: number;
  estoqueMinimo: { sku: string; descricao: string; saldo: number; estoqueMinimo: number }[];
  saldosPendentes: { numero: number; total: number; pago: number }[];
};
const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

export function Reports({ token }: { token: string }) {
  const [start, setStart] = useState(() => new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10));
  const [end, setEnd] = useState(() => new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Report | null>(null);
  const [error, setError] = useState('');
  async function load() {
    try {
      const params = new URLSearchParams({ inicio: new Date(`${start}T00:00:00`).toISOString(), fim: new Date(`${end}T23:59:59`).toISOString() });
      setReport(await request<Report>(`/api/relatorios/resumo?${params}`, token)); setError('');
    } catch (error) { setError(String(error)); }
  }
  useEffect(() => { void load(); }, [token]);
  return <>
    <SectionHeader eyebrow="Indicadores" title="Relatórios" description="Consulte a operação e o financeiro no período escolhido." />
    <Paper sx={{ p: 2.5, mb: 3 }}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}><Typography sx={{ flex: 1, fontWeight: 750 }}>Período de análise</Typography><TextField size="small" label="Início" type="date" InputLabelProps={{ shrink: true }} value={start} onChange={event => setStart(event.target.value)} /><TextField size="small" label="Fim" type="date" InputLabelProps={{ shrink: true }} value={end} onChange={event => setEnd(event.target.value)} /><Button variant="contained" onClick={() => void load()}>Consultar</Button></Stack></Paper>
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    {report && <>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 2.5, mb: 3 }}><Paper sx={{ p: 3, color: '#fff', border: 0, background: 'linear-gradient(145deg, #123848, #0e6c70)' }}><Typography sx={{ color: '#b4d9d8', fontSize: 12, fontWeight: 750 }}>FATURAMENTO</Typography><Typography sx={{ fontSize: 32, fontWeight: 800, mt: 1 }}>{money(report.faturamento)}</Typography></Paper><Paper sx={{ p: 3 }}><Typography color="text.secondary" sx={{ fontSize: 12, fontWeight: 750 }}>TICKET MÉDIO</Typography><Typography sx={{ fontSize: 32, fontWeight: 800, mt: 1 }}>{money(report.ticketMedio)}</Typography></Paper></Box>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, 1fr)' }, gap: 2.5 }}>
        <ReportCard title="Ordens por status">{report.porStatus.length === 0 ? <Typography color="text.secondary">Sem ordens no período.</Typography> : report.porStatus.map(x => <Stack key={x.status} direction="row" alignItems="center" justifyContent="space-between" sx={{ py: 1, borderBottom: '1px solid #edf0f1' }}><StatusChip status={x.status}/><Typography fontWeight={750}>{x.quantidade}</Typography></Stack>)}<Typography color="text.secondary" sx={{ mt: 2, fontSize: 13 }}>Retornos em garantia: {report.retornosGarantia}</Typography></ReportCard>
        <ReportCard title="Pagamentos por forma">{report.pagamentosPorForma.length === 0 ? <Typography color="text.secondary">Sem pagamentos no período.</Typography> : report.pagamentosPorForma.map(x => <Stack key={x.forma} direction="row" justifyContent="space-between" gap={2} sx={{ py: 1, borderBottom: '1px solid #edf0f1' }}><Typography>{x.forma} <Box component="span" sx={{ color: 'text.secondary', fontSize: 12 }}>({x.movimentos})</Box></Typography><Typography fontWeight={750}>{money(x.valor)}</Typography></Stack>)}</ReportCard>
        <ReportCard title="Produtividade por técnico">{report.porTecnico.length === 0 ? <Typography color="text.secondary">Sem OS atribuídas no período.</Typography> : report.porTecnico.map(x => <Stack key={x.tecnicoId} direction="row" justifyContent="space-between" sx={{ py: 1, borderBottom: '1px solid #edf0f1' }}><Typography>{x.nome}</Typography><Typography fontWeight={750}>{x.quantidade} OS</Typography></Stack>)}</ReportCard>
        <ReportCard title="Saldos pendentes">{report.saldosPendentes.length === 0 ? <Typography color="text.secondary">Nenhum saldo pendente.</Typography> : report.saldosPendentes.map(x => <Stack key={x.numero} direction="row" justifyContent="space-between" sx={{ py: 1, borderBottom: '1px solid #edf0f1' }}><Typography>OS #{x.numero}</Typography><Typography fontWeight={750}>{money(x.total - x.pago)}</Typography></Stack>)}</ReportCard>
      </Box>
      <Box sx={{ mt: 2.5 }}>{report.estoqueMinimo.length === 0 ? <EmptyState icon="stock" title="Estoque sem alertas" description="Nenhuma peça está no estoque mínimo neste período." /> : <ReportCard title="Peças no estoque mínimo">{report.estoqueMinimo.map(x => <Stack key={x.sku} direction="row" justifyContent="space-between" gap={2} sx={{ py: 1, borderBottom: '1px solid #edf0f1' }}><Typography>{x.sku} · {x.descricao}</Typography><Typography fontWeight={750}>{x.saldo} / {x.estoqueMinimo}</Typography></Stack>)}</ReportCard>}</Box>
    </>}
  </>;
}

function ReportCard({ title, children }: { title: string; children: ReactNode }) { return <Paper sx={{ p: 3 }}><Typography variant="h6" sx={{ mb: 2 }}>{title}</Typography>{children}</Paper>; }
