# Auditoria e matriz de testes — IT Digitação 2.0.0-rc.1

A referência `backup/pre-release-2.0.0` aponta para `6a0f60a`, a base WPF x86/.NET Framework 4.8, sem pacotes NuGet. A base compilou e passou pelas suítes Core e WPF no Windows Server 2022. [Linha de base](https://github.com/NathanCapoeira/IT-digitacao-train/actions/runs/45774057006)

| Inventário inicial | Classificação |
| --- | --- |
| Início, seis durações, biblioteca, dez categorias, busca, filtros, aleatoriedade, editor e navegação | Implementado e testado em CI |
| Sessões, precisão, PPM, edição Unicode, progresso e resumo | Implementado e testado em CI |
| Guia ABNT2, preferências, perguntas e conquistas | Implementado e testado parcialmente em CI |
| Combinações reais de DPI, ABNT2 físico, Windows cliente, som audível, teste pedagógico | Implementado, com alguns cenários testados por automação e validação presencial pendente |
| Contagem de caracteres e elegibilidade de estrelas | P1 corrigido: repetições inflavam a contagem e podiam liberar estrelas |
| Carregamento da biblioteca e das artes | P2 corrigido: carregamento antecipado, decodificações repetidas e tratamento insuficiente de ilustração ausente |
| Pacote portátil verificável, documentação para professores e ícone do PE | Ausente na base e implementado nesta atualização |
| Instalador | Ausente. O piloto usa o pacote portátil. |

## Matriz

A candidata foi compilada do commit `ba7007a83ee97b3b6ca2e2aee010a9c8cce4d5d7`. O build passou em 127 verificações Core e 63 verificações WPF, sem falhas nesses testes. [Execução 45775428024](https://github.com/NathanCapoeira/IT-digitacao-train/actions/runs/45775428024) Das 50 atividades, há 20 iniciantes, 15 intermediárias e 15 avançadas, cinco por categoria.

| ID | Procedimento | Esperado | Observação / evidência | Estado | Ambiente |
| --- | --- | --- | --- | --- | --- |
| BASE-01 | Verificar build de referência | Release x86 e suítes verdes | Execução 45774057006 | Aprovado | Windows Server 2022 |
| CAT-01 | Validar catálogo, categorias, filtros e buscas no Core | 50 textos, dez categorias, IDs únicos, faixas válidas, consultas consistentes | `smoke-core.ps1`, suíte com 127 verificações | Aprovado | Windows Server 2022 |
| MOTOR-01 | Editar e comparar textos, validar Unicode, métricas e repetição | Precisão preserva histórico; repetição não duplica crédito nem libera estrelas | `smoke-core.ps1`, suíte com 127 verificações | Aprovado | Windows Server 2022 |
| SESS-01 | Simular seis durações, encerrar, trocar, reiniciar e abandonar | Relógio contínuo, sem duplicar conclusão, bloqueio após término | `smoke-core.ps1`, suíte com 127 verificações | Aprovado | Windows Server 2022 |
| SESS-02 | Aguardar cinco minutos no motor com relógio monotônico real | Encerra no limite e ignora entrada tardia | Limite atingido em 300,097 s no Core e entrada tardia recusada. `real-session.json` | Aprovado | Windows Server 2022 |
| UI-01 | Executar suíte gráfica e capturar as sete telas | Imagens, navegação, biblioteca, editor, preferências, guia e resultados funcionam | 63 verificações, incluindo durações exclusivas, layout compacto, editor, comando WPF de colagem, guia e resumo. Capturas do build | Aprovado | Windows Server 2022 |
| ZIP-01 | Verificar PE x86, metadados, ícone, extrair e comparar hashes de cada arquivo | Pacote íntegro e vinculado ao commit real | `BUILD-INFO.json`, `SHA256SUMS.txt`, logs | Aprovado | Windows Server 2022 |
| RUN-01 | Abrir o executável extraído três vezes com bloqueio de rede por programa onde efetivo | Janela abre e fecha; métricas do runner medidas | Três aberturas e fechamentos normais com entrada e saída de rede bloqueadas para o executável. `runtime.json` | Aprovado | Windows Server 2022 |
| SUP-01 | Compilar o motor isolado e testar diacríticos, digitação sucessiva, precisão de 90/100 e catálogos inválidos | Dez verificações aprovadas | PowerShell em Linux, código recompilado isoladamente | Aprovado | Linux |
| MAN-01 | Matriz física de DPI, ABNT2, acessibilidade, áudio, piloto e recursos em máquina de 2 GB | Usabilidade e estabilidade em sala | Checklist presencial | Pendente de validação humana | Equipamentos do Instituto |
| SO-01 | Executar em Windows 7 SP1, Windows 10 e Windows 11 | Compatibilidade confirmada por sistema | Checklist presencial | Pendente de validação humana | Sistemas cliente |
| PED-01 | Revisar fatos, fontes e clareza dos textos com professores | Aprovação pedagógica informada | Catálogo sem referências fabricadas. Consultas institucionais bloqueadas pelo proxy | Pendente de validação humana | Instituto |

## Desempenho e integridade

Runner Windows Server 2022 Datacenter x64, compilação 10.0.20348, AMD EPYC 7763 e 16.379 MiB de RAM visível. A inicialização mede o tempo até aparecer a janela principal. A memória foi medida após dois segundos em repouso.

| Abertura | Janela principal | Working set | Memória privada |
| --- | --- | --- | --- |
| 1 | 299 ms | 59,73 MiB | 41,63 MiB |
| 2 | 253 ms | 59,45 MiB | 40,53 MiB |
| 3 | 251 ms | 59,45 MiB | 40,52 MiB |

As medições descrevem este runner. Tempo até a janela, memória em uso ativo e estabilidade ao longo de sessões repetidas em máquinas do Instituto aguardam medições locais. O bloqueio de rede no runner foi aplicado somente ao executável durante as três aberturas. O checklist presencial cobre o fluxo completo com a rede desconectada, teclado físico, áudio, Tab/Shift+Tab, atalhos de colagem, DPI e ícone na barra de tarefas.

O ZIP público tem 1.526.787 bytes. O conjunto de arquivos extraídos ocupa 1.635.629 bytes antes do `BUILD-INFO.json`. O SHA-256 foi conferido após baixar a publicação e o conteúdo passou no teste de extração. [Pré-release e evidências](https://github.com/NathanCapoeira/IT-digitacao-train/releases/tag/v2.0.0-rc.1)

```text
41d4b0fa5f35a8a0b4c68ba21e21eadadbd1b8df9baf6db64b3b9a9694e3ba12
```

## Revisão de conteúdo e pendências

Os 50 textos passaram por checagens de UTF-8, NFC, completude, fins de frase, IDs e títulos únicos, categorias válidas e faixas de tamanho. O catálogo foi preservado. Na revisão humana, priorizar recomendações de higiene e alimentação, alegações de segurança digital, conceitos financeiros e fatos científicos e geográficos. A revisão editorial e o piloto permanecem abertos, assim como a meta de 100 textos. O ambiente retornou HTTP 403 nas consultas institucionais.

O workflow sinalizou a migração automática de algumas ações do Node.js 20 para Node.js 24. As etapas terminaram com sucesso. O pacote portátil é o formato desta candidata. O instalador fica pendente de avaliação para as máquinas legadas.
