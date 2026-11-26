# Auditoria e matriz de testes — IT Digitação 2.0.0-rc.1

A referência `backup/pre-release-2.0.0` aponta para `6a0f60a`, a base WPF x86/.NET Framework 4.8, sem pacotes NuGet. A base compilou e passou pelas suítes Core e WPF no Windows Server 2022. [Linha de base](https://github.com/NathanCapoeira/IT-digitacao-train/actions/runs/45774057006)

| Inventário inicial | Classificação |
| --- | --- |
| Início, seis durações, biblioteca, dez categorias, busca, filtros, aleatoriedade, editor e navegação | Implementado e testado em CI |
| Sessões, precisão, PPM, edição Unicode, progresso e resumo | Implementado e testado em CI |
| Guia ABNT2, preferências, perguntas e conquistas | Implementado e testado parcialmente em CI |
| Combinações reais de DPI, ABNT2 físico, Windows cliente, som audível, teste pedagógico | Implementado, em parte testado por automação; validação presencial pendente |
| Contagem de caracteres e elegibilidade de estrelas | P1 corrigido: repetições inflavam a contagem e podiam liberar estrelas |
| Carregamento da biblioteca e das artes | P2 corrigido: carregamento antecipado, decodificações repetidas e tratamento insuficiente de ilustração ausente |
| Pacote portátil verificável, documentação para professores e ícone do PE | Ausente na base; implementado nesta atualização |
| Instalador | Ausente; adotado pacote portátil para o piloto |

## Matriz

Os resultados da candidata estão vinculados aos logs e arquivos de evidência do próprio build. A coluna de ambiente descreve a execução efetiva. Das 50 atividades, há 20 iniciantes, 15 intermediárias e 15 avançadas, cinco por categoria.

| ID | Procedimento | Esperado | Observação / evidência | Estado | Ambiente |
| --- | --- | --- | --- | --- | --- |
| BASE-01 | Verificar build de referência | Release x86 e suítes verdes | Execução 45774057006 | Aprovado | Windows Server 2022 |
| CAT-01 | Validar catálogo, categorias, filtros e buscas no Core | 50 textos, dez categorias, IDs únicos, faixas válidas, consultas consistentes | `smoke-core.ps1` | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| MOTOR-01 | Editar e comparar textos, validar Unicode, métricas e repetição | Precisão preserva histórico; repetição não duplica crédito nem libera estrelas | `smoke-core.ps1` | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| SESS-01 | Simular seis durações, encerrar, trocar, reiniciar e abandonar | Relógio contínuo, sem duplicar conclusão, bloqueio após término | `smoke-core.ps1` | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| SESS-02 | Aguardar cinco minutos no motor com relógio monotônico real | Encerra no limite e ignora entrada tardia | `real-session.json` | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| UI-01 | Executar suíte gráfica e capturar as sete telas | Imagens, navegação, biblioteca, editor, preferências, guia e resultados funcionam | `smoke-wpf.ps1` e capturas do build | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| ZIP-01 | Verificar PE x86, metadados, ícone, extrair e comparar hashes de cada arquivo | Pacote íntegro e vinculado ao commit real | `BUILD-INFO.json`, `SHA256SUMS.txt`, logs | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| RUN-01 | Abrir o executável extraído três vezes com bloqueio de rede por programa onde efetivo | Janela abre e fecha; métricas do runner medidas | `runtime.json` | Não executado neste relatório estático; consultar execução vinculada | Windows Server 2022 |
| MAN-01 | Matriz física de DPI, ABNT2, acessibilidade, áudio, piloto e recursos em máquina de 2 GB | Usabilidade e estabilidade em sala | Checklist presencial | Pendente de validação humana | Equipamentos do Instituto |
| SO-01 | Executar em Windows 7 SP1, Windows 10 e Windows 11 | Compatibilidade confirmada por sistema | Checklist presencial | Pendente de validação humana | Sistemas cliente |
| PED-01 | Revisar fatos, fontes e clareza dos textos com professores | Aprovação pedagógica informada | Catálogo sem referências fabricadas. Consultas institucionais bloqueadas pelo proxy | Pendente de validação humana | Instituto |
