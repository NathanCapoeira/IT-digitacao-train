# IT Digitação Legacy — estado da migração

A Standard em `src/ITDigitacao.App/` é a referência funcional e visual. O Legacy reutiliza o `ITDigitacao.Legacy.Core`, o catálogo e os recursos originais, e constrói a interface em WPF programático para evitar a etapa XAML/BAML que falhou com as bibliotecas WPF antigas. Os comportamentos abaixo foram comparados com os code-behind e XAML Standard durante a implementação. Ainda não há validação visual automatizada por captura nem teste presencial desta compilação.

| Área da Standard | Estado implementado no Legacy | Diferença e validação pendente |
|---|---|---|
| Início | Banner com mascote, seleção de duração de 5 a 30 minutos, início de sessão e benefícios. | Os cards e ícones foram recriados em código. Espaçamentos, tipografia e comportamento responsivo precisam de comparação visual com a Standard. |
| Biblioteca | Carrega o catálogo integral; exibe categorias ilustradas e atividades, pesquisa, filtro por dificuldade, estado vazio, seleção aleatória, retomada e encerramento da sessão. | Os cards são construídos individualmente em vez dos `ItemsControl`/`DataTemplate` originais. A aparência e os estados de pesquisa/categoria precisam ser conferidos em tela. |
| Editor de digitação | Usa o mesmo motor, texto por elementos Unicode, destaque de acertos/erros/próximo caractere, bloqueio de colagem, métricas, progresso, contagem regressiva, avisos, fonte ajustável, som, concentração, reinício, retorno/abandono e encerramento. | O acompanhamento automático do trecho de referência e os mesmos detalhes de layout/altura do editor Standard ainda não foram reproduzidos. Os handlers e comandos precisam ser validados em WPF 3.5 real. |
| Perguntas educativas | Usa a pergunta, opções, resposta correta e explicação existentes no catálogo; permite responder ou pular antes de retornar à biblioteca. | A tela usa controles programáticos próprios. Ainda falta confirmar interação por teclado/leitor assistivo e equivalência visual. |
| Resultados | Exibe mensagem, estrelas pelas regras `SessionAwards`, seis métricas, três conquistas e ações de nova atividade/início/saída. | Conteúdo e regras foram portados; cartões e animação não foram comparados renderizados com a Standard. |
| Guia ABNT2 | Apresenta teclado colorido por dedo, realce de teclas pressionadas, orientação de postura e exercício da fileira inicial. | É uma reconstrução programática. O mapeamento completo de teclas ABNT2 e a correspondência de posição/dedo precisam de teste presencial. |
| Configurações | Preferências JSON de fonte, som, métricas, concentração, feedback visual, animações e efeitos reduzidos; salva no perfil do usuário e permite restaurar padrões. | A persistência foi exercitada no pipeline anterior. O round-trip das opções, retorno à tela/atividade e sua aplicação em Windows 7 ainda precisam de validação presencial. |
| Catálogo e núcleo | Core Legacy separado para .NET 3.5; catálogo original com 10 categorias e 50 atividades, busca e serialização JSON. | Os testes automatizados de catálogo e motor devem passar no workflow de cada commit. |
| Atalho e distribuição | `Criar-Atalho.exe`, projetos x86, pacote portátil e workflow independentes do Standard. | A criação do atalho e os requisitos de unidade removível precisam de teste no Windows do instituto. |

## Critério de versão

Esta compilação não deve ser chamada de RC. A equivalência visual integral ainda não foi comprovada, há diferenças de layout explicitadas acima e não houve teste presencial da compilação atual no Windows 7 SP1. A versão distribuída pelo workflow permanece Beta até essas verificações serem concluídas.

## Decisão sobre XAML

O Legacy mantém a infraestrutura WPF sem compilação XAML/BAML da prova de conceito. Os XAML e code-behind Standard foram usados como especificação de conteúdo, regras, eventos e apresentação. Os controles e templates foram reexpressos programaticamente; não se carrega XAML Standard dinamicamente, pois recursos, nomes, bindings, templates e handlers dependem de recursos de framework e code-behind que não foram validados no runtime 3.5. Portanto, esta é uma migração de comportamento e aparência, não uma conversão literal já comprovada dos arquivos XAML.
