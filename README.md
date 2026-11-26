# IT Digitação

**Instituto de Transformação • Fases 6 a 8 • Versão 1.3.0**

*Aprenda. Pratique. Transforme.*

O IT Digitação é um aplicativo educacional para aproveitar o tempo restante da aula com atividades de digitação. A proposta é desenvolver precisão, concentração, leitura e vocabulário, com conteúdo voltado principalmente a crianças e adolescentes.

## Tela inicial

Escolha quanto tempo você tem para praticar, de 5 a 30 minutos. **Começar minha aventura** abre a Biblioteca, com dez categorias ilustradas e cinquenta textos em português. Pesquise pelo tema ou título e refine a busca por dificuldade. Você pode abrir uma categoria ou selecionar uma atividade ao acaso entre os resultados atuais.

O cronômetro começa no primeiro caractere avaliado. Ele continua enquanto você navega ou minimiza a janela. O editor aceita edição manual e impede colagem e arraste de texto nesse campo. Em **Retomar texto atual**, a digitação é preservada. Escolher outro texto encerra a tentativa atual e mantém suas métricas na sessão. Use **A−/A+** para ajustar a fonte e **Concentração** para liberar espaço. O guia ABNT2 destaca teclas apenas na janela ativa e oferece um exercício que usa a sessão atual. O resumo dá estrelas após pelo menos 100 caracteres avaliados (uma abaixo de 80%, duas de 80% a menos de 95% e três a partir de 95%). A velocidade não interfere nas conquistas. Algumas atividades incluem uma pergunta opcional após a conclusão.

### Métricas e conteúdo

- **Precisão** é a proporção de tentativas de caracteres corretas. Erros corrigidos continuam no denominador. Remoções, navegação e desfazer/refazer não entram como tentativas.
- **PPM líquida** usa cinco caracteres corretos por palavra. Divide-se o total pelo tempo corrido da sessão desde a primeira tentativa, inclusive pausas, navegação e inatividade. Para cada atividade conta o maior saldo registrado ao sair dela ou o saldo atual se for maior. Redigitar o mesmo trecho ou concluir o mesmo texto outra vez não aumenta o total por atividade. Até completar dez segundos, a PPM aparece como “—”.
- **Progresso** mede caracteres corretos na posição esperada. A conclusão exige igualdade integral após normalizar acentos equivalentes em Unicode e as quebras de linha. Letras maiúsculas, espaços e pontuação continuam significativos.
- **Estimativa de leitura e digitação** usa 20 palavras por minuto e arredonda para cima. O tempo real depende do ritmo do aluno.
- O catálogo em `content/atividades.json` contém textos autorais. As tentativas de consulta a IBGE Educa, Banco Central, CERT.br, Embrapa, Agência Espacial Brasileira e Ministério da Saúde foram bloqueadas neste ambiente. O campo `Sources` fica vazio enquanto aguarda revisão factual. O acervo tem cinco textos por categoria, com dois iniciantes em cada uma, quinze intermediários e quinze avançados no total. A meta de 100 fica para uma expansão com revisão editorial.

As configurações são gravadas em `%LOCALAPPDATA%\Instituto de Transformação\IT Digitação\preferencias.json`. Um arquivo inválido carrega os padrões. O som é local, opcional e desligado por padrão. Tempo, respostas, desempenho e conquistas ficam em memória durante a sessão.

### Baixar o aplicativo

Na página [GitHub Actions](https://github.com/NathanCapoeira/IT-digitacao-train/actions), abra a execução bem-sucedida mais recente de **Build do IT Digitação**. Com sua conta do GitHub conectada, baixe **IT-Digitacao-Train-1.3.0-Release** na seção **Artifacts**. Extraia todos os arquivos e abra `IT-Digitacao.exe` na pasta extraída.

## Estrutura

| Caminho | Conteúdo |
| --- | --- |
| `ITDigitacao.sln` | Solução MSBuild com configuração Release x86 |
| `src/ITDigitacao.App/` | Aplicativo WPF em C# e XAML para .NET Framework 4.8 |
| `src/ITDigitacao.App/Themes/` | Paleta, ícones vetoriais e componentes compartilhados |
| `src/ITDigitacao.App/Views/HomeView.xaml` | Banner, escolha de duração e benefícios |
| `src/ITDigitacao.App/Assets/` | Mascote e marcas com Build Action Resource |
| `src/ITDigitacao.Core/` | Catálogo JSON, comparação de caracteres, relógio e sessão independentes de WPF |
| `tests/` | Testes do Core com relógio simulado e integração WPF no Windows |
| `scripts/build.ps1` | Descoberta do MSBuild, compilação limpa e preparação da distribuição |
| `.github/workflows/build-windows.yml` | Compilação no Windows e publicação do artefato de CI |

## Pré-requisitos no Windows

1. Instale Git e [Visual Studio Code](https://code.visualstudio.com/). Se desejar usar o comando `code`, habilite a opção de adicionar o VS Code ao PATH no instalador.
2. Na página de downloads da Microsoft, instale o **Build Tools for Visual Studio 2022**. No Visual Studio Installer, selecione a carga de trabalho **.NET desktop build tools**. Em **Componentes individuais**, confirme o MSBuild, o compilador C# e o **.NET Framework 4.8 targeting pack**. Basta instalar o Build Tools.
3. Se o pacote de referência v4.8 estiver ausente, instale o [Developer Pack do .NET Framework 4.8](https://dotnet.microsoft.com/pt-br/download/dotnet-framework/net48), que fornece as referências para compilação. A máquina que apenas executa o aplicativo precisa do runtime .NET Framework 4.8 ou de uma versão 4.x posterior que o substitua.
4. Use o **Windows PowerShell 5.1** no terminal integrado do VS Code. A extensão C# é opcional para edição. O script descobre o MSBuild por meio do `vswhere`, com o PATH como alternativa.

[Downloads do Build Tools](https://visualstudio.microsoft.com/pt-br/downloads/)

## Compilar e executar

Para obter o código e compilar, rode todo o bloco no PowerShell:

```powershell
git clone https://github.com/NathanCapoeira/IT-digitacao-train.git
Set-Location IT-digitacao-train
code .
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
if ($LASTEXITCODE -ne 0) { throw "O build falhou. Consulte a saída acima." }
Get-Item .\dist\IT-Digitacao.exe
& .\dist\IT-Digitacao.exe
```

Para um checkout existente atualizado, comece na raiz do projeto e execute a partir de `code .`. A opção `-ExecutionPolicy Bypass` vale somente para o processo PowerShell iniciado pelo comando.

O script chama o MSBuild com `Rebuild`, `Configuration=Release` e `Platform=x86`. A pasta `dist` é preparada a partir de uma compilação bem-sucedida e contém `IT-Digitacao.exe`, `IT-Digitacao.exe.config`, `ITDigitacao.Core.dll` e `atividades.json`. Mantenha todos na mesma pasta.

### Diagnóstico

- **MSBuild ausente** — revise a carga de trabalho .NET desktop build tools no Visual Studio Installer.
- **Referências v4.8 ausentes ou MSB3644** — instale o Developer Pack ou o targeting pack **4.8**.
- **Falha do MSBuild** — examine o primeiro erro informado no terminal. Para salvar a saída mantendo o código de saída do processo:

  ```powershell
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 2>&1 |
      Tee-Object -FilePath "$env:TEMP\IT-Digitacao-build.log"
  $buildExitCode = $LASTEXITCODE
  Get-Item "$env:TEMP\IT-Digitacao-build.log"
  if ($buildExitCode -ne 0) { throw "Falha de compilação ($buildExitCode)." }
  ```

  Após uma falha, a pasta `dist` pode ainda conter uma distribuição anterior. Só use o executável como resultado da compilação atual se o comando terminar com código zero.

## GitHub Actions

O workflow **Build do IT Digitação** roda em Windows nas atualizações de `main`, nos pull requests para `main` e por disparo manual em **Actions → Build do IT Digitação → Run workflow**. Após um job bem-sucedido, baixe **IT-Digitacao-Train-1.3.0-Release** na seção **Artifacts** da execução. O arquivo `build-info.txt` registra o commit compilado. A execução publica sete capturas geradas pelo teste WPF no Windows, da tela inicial às configurações. A distribuição contém a versão de desenvolvimento.

## Validação no Windows

Após um build bem-sucedido, abra o executável e confirme:

1. Escolha uma duração e abra a Biblioteca. Confira categorias, títulos, filtros e pesquisa.
2. Abra um texto, digite com acentos e corrija um erro. Confira se precisão e contagem preservam a tentativa anterior.
3. Volte à Biblioteca e retome a digitação. Confira que o tempo permanece em andamento.
4. Encerre a sessão, confira o resumo e inicie outra sessão. Teste em janelas estreitas e nas escalas de DPI da instituição.

A validação de Windows 7 SP1 depende dos pré-requisitos do framework e de teste real. Compatibilidade nas versões alvo, uso em máquinas de 2 GB de RAM e comportamento nas resoluções e escalas de DPI previstas são metas de validação.
