using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.ServiceProcess;

class Program
{
    static string[] group1Folders = {
        @"C:\Temp_Arq_Calc_Tax\Arq_Gamma\Processed",
        @"C:\Temp_Arq_Calc_Tax\Arq_Sovos\Request\Processed"
    };

    static string[] group2Folders = {
        @"C:\Temp_Arq_Calc_Tax\Arq_Sovos\Responser\Processed",
       /// @"C:\Temp_Arq_Calc_Tax\Arq_Cone\IN\Erro",
        @"C:\Temp_Arq_Calc_Tax\Arq_Cone\IN\Processed"
    };

    static string[] group3Folders = {
        @"C:\Temp_Arq_Calc_Tax\Arq_Gamma\IN",
        @"C:\Temp_Arq_Calc_Tax\Arq_Sovos\Responser"
    };


    static string logFilePath = @"C:\Temp_Arq_Calc_Tax\Log_Taxrule\log.txt";
    static string erro_file = @"C:\Temp_Arq_Calc_Tax\Log_Taxrule\Erro_File";
    static Timer program0Timer;
    static DateTime lastDeletionTimeG1 = DateTime.MinValue;
    static DateTime lastDeletionTimeG2 = DateTime.MinValue;
    static DateTime lastDeletionTimeG3 = DateTime.MinValue;

    static void Main(string[] args)
    {
        if (Environment.UserInteractive)
        {
            using (StreamWriter logWriter = new StreamWriter(logFilePath, true))
            {
                program0Timer = new Timer(state =>
                {
                    // Iniciar o Program0
                    Start10minutos(logWriter);
                    CleanProgram4Horas(logWriter);

                    // Desligar o Program2
                    // StopProgram0(logWriter);

                    // Agendar o próximo desligamento do Program0
                    ScheduleProgram0Shutdown(logWriter);
                }, logWriter, TimeSpan.Zero, TimeSpan.FromDays(1));

                Console.WriteLine("Pressione Enter para sair...");
                Console.ReadLine();

                program0Timer.Dispose();
            }

        }
        else
        {
            ServiceBase.Run(new TaxService());
        }
    }

    public class TaxService : ServiceBase
    {
        private StreamWriter logWriter;
        private Timer program0Timer;
        private CancellationTokenSource cancellationTokenSource;

        public TaxService()
        {
            ServiceName = "TaxService";
        }

        protected override void OnStart(string[] args)
        {
            logWriter = new StreamWriter(logFilePath, true);
            cancellationTokenSource = new CancellationTokenSource();

            program0Timer = new Timer(state =>
            {
                try
                {
                    StartProgram0(logWriter);
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine("Erro: " + ex.ToString());
                }
            }, logWriter, TimeSpan.Zero, TimeSpan.FromMinutes(10)); // Altere o intervalo conforme necessário

            // Executar a limpeza a cada 4 horas
            var cleanupTimer = new Timer(state =>
            {
                try
                {
                    CleanProgram(logWriter);
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine("Erro: " + ex.ToString());
                }
            }, logWriter, TimeSpan.Zero, TimeSpan.FromHours(4)); // Altere o intervalo conforme necessário
        }

        protected override void OnStop()
        {
            // Sinaliza o cancelamento das tarefas em andamento
            cancellationTokenSource?.Cancel();

            // Cancela a execução dos timers
            program0Timer?.Change(Timeout.Infinite, Timeout.Infinite);

            // Executa o método StopProgram0
            StopProgram0(logWriter);

            // Aguarda a conclusão das tarefas em andamento (se houver)
            Task.WhenAll(/*Coloque aqui as tasks que precisam ser aguardadas*/)
                .ContinueWith(t =>
                {
                    // Realiza a limpeza necessária antes de encerrar o serviço.
                    CleanupBeforeExit(logWriter);

                })
                .Wait();

            // Encerra o serviço
            base.OnStop();
        }

        private static void CleanupBeforeExit(StreamWriter logWriter)
        {
            // Implemente a limpeza necessária antes de encerrar o programa.
            // Por exemplo, finalize processos em execução, feche conexões, etc.
            StopProgram0(logWriter);

            // Feche o logWriter antes de sair
            logWriter.Dispose();

            // Exiba uma mensagem de encerramento do programa, se necessário
            Console.WriteLine("Encerrando o programa. Pressione Enter novamente para sair...");
        }
    }












    public class LogWriter : TextWriter
    {
        private StreamWriter logFileWriter;

        public LogWriter(string logFilePath)
        {
            logFileWriter = new StreamWriter(logFilePath, true);
        }

        public override void WriteLine(string value)
        {
            Console.WriteLine(value); // Imprimir a mensagem no console

            // Escrever a mensagem no arquivo de log
            logFileWriter.WriteLine(value);
            logFileWriter.Flush(); // Certificar-se de que a mensagem seja gravada no arquivo imediatamente
        }

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                logFileWriter.Dispose();
            }

            base.Dispose(disposing);
        }
    }








    private static async Task WaitForDirectoryEmptyOrChangedAsync(string directoryPath, TimeSpan timeout, StreamWriter logWriter)
    {
        DateTime startTime = DateTime.Now;
        bool isDirectoryEmptyOrChanged = false;

        while (!isDirectoryEmptyOrChanged && (DateTime.Now - startTime) < timeout)
        {
            await Task.Delay(TimeSpan.FromSeconds(10)); // Verificar a cada 10 segundos se o diretório está vazio ou foi alterado

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            FileInfo[] files = directory.GetFiles();

            if (files.Length == 0)
            {
                isDirectoryEmptyOrChanged = true;
            }
            else
            {
                logWriter.WriteLine($"O diretório está sendo abastecido {directoryPath}. Aguarde até o término...");
            }
        }

        if (!isDirectoryEmptyOrChanged)
        {
            logWriter.WriteLine($"O diretório {directoryPath} não ficou vazio ou não teve alterações dentro do tempo limite especificado.");
        }
    }


    static async Task RunProcesses3Async(CancellationToken cancellationToken)
    {
        if (DateTime.Now.Subtract(lastDeletionTimeG1).TotalHours >= 1)
        {
            foreach (string folderPath in group1Folders)
            {
                await DeleteFilesInFolderAsync(folderPath);
                // Console.WriteLine(logWriter); // Adiciona a linha no console
            }
            lastDeletionTimeG1 = DateTime.Now;
        }

        if (DateTime.Now.Subtract(lastDeletionTimeG2).TotalHours >= 2)
        {
            foreach (string folderPath in group2Folders)
            {
                await DeleteFilesInFolderAsync(folderPath);
                // Console.WriteLine(logWriter); // Adiciona a linha no console
            }
            lastDeletionTimeG2 = DateTime.Now;
        }

        // Verifica se o cancelamento foi solicitado
        if (cancellationToken.IsCancellationRequested)
        {
            //logWriter.WriteLine("RunProcesses3Async canceled.");
            Console.WriteLine("RunProcesses3Async canceled.");
            return;
        }
    }



    static async Task RunProcess(StreamWriter logWriter, string fileName, CancellationToken cancellationToken)
    {
        const long MAX_LOG_SIZE = 1000000; // Tamanho máximo do arquivo de log em bytes

        string processName = Path.GetFileNameWithoutExtension(fileName);

        // Verificar se o processo já está em execução
        if (IsProcessRunning(processName))
        {
            logWriter.WriteLine($"Process {processName} is already running.");
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true; // Redireciona a saída de erro
            startInfo.UseShellExecute = false;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string logLine = $"{DateTime.Now} [{fileName}] {e.Data}"; // Adiciona a data, nome do executável e a linha de saída
                        Console.WriteLine(e.Data); // Exibe a saída padrão no console

                        if (!logWriter.BaseStream.CanWrite)
                        {
                            return; // O StreamWriter foi fechado, não escreva mais no arquivo de log
                        }

                        if (logWriter.BaseStream.Length + logLine.Length < MAX_LOG_SIZE)
                        {
                            logWriter.WriteLine(logLine); // Grava a saída padrão no arquivo de log
                        }
                        else
                        {
                            // Realize a ação apropriada, como criar um novo arquivo de log ou arquivar o arquivo existente
                            // Exemplo: logWriter.Dispose(); e criar um novo StreamWriter para um novo arquivo de log
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string logLine = $"{DateTime.Now} [{fileName}] ERROR: {e.Data}"; // Adiciona a data, nome do executável e a linha de erro
                        Console.WriteLine(e.Data); // Exibe o erro no console

                        if (!logWriter.BaseStream.CanWrite)
                        {
                            return; // O StreamWriter foi fechado, não escreva mais no arquivo de log
                        }

                        if (logWriter.BaseStream.Length + logLine.Length < MAX_LOG_SIZE)
                        {
                            logWriter.WriteLine(logLine); // Grava o erro no arquivo de log
                        }
                        else
                        {
                            // Realize a ação apropriada, como criar um novo arquivo de log ou arquivar o arquivo existente
                            // Exemplo: logWriter.Dispose(); e criar um novo StreamWriter para um novo arquivo de log
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine(); // Inicia a leitura da saída de erro

                // Aguarde o token de cancelamento ou um atraso específico antes de encerrar o processo
                await Task.WhenAny(Task.Delay(60000, cancellationToken), process.WaitForExitAsync());

                if (cancellationToken.IsCancellationRequested)
                {
                    process.Kill(); // Encerra o processo
                    logWriter.WriteLine($"[{fileName}] Application killed at {DateTime.Now}");
                }
                else
                {
                    logWriter.WriteLine($"[{fileName}] Processo encerrado.");
                }
            }
        }
        catch (Exception ex)
        {
            logWriter.WriteLine($"Error running process for file {fileName}: {ex.Message}");
        }
        //finally
        //{
        //    logWriter.Dispose(); // Libera os recursos do StreamWriter
        //}
    }

    static string GetNewLogFileName(string fileName)
    {
        string baseDirectory = Path.GetDirectoryName(fileName);
        string baseFileName = Path.GetFileNameWithoutExtension(fileName);
        string newFileName = $"{baseFileName}_{DateTime.Now:yyyyMMddHHmmss}.log"; // Cria um novo nome de arquivo baseado na data e hora atual
        return Path.Combine(baseDirectory, newFileName); // Retorna o caminho completo para o novo arquivo de log
    }

    static bool IsProcessRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        return processes.Length > 0;
    }




    static async Task Start10minutos(StreamWriter logWriter)
    {
        while (true)
        {
            await StartProgram0(logWriter);
            TimeSpan delayInterval = TimeSpan.FromMinutes(10); ////----> alterar para 10 minutos quando aplicar em prd
            await Task.Delay(delayInterval);
            // Limpar o arquivo de log
            //logWriter.Flush();
        }
    }



    static async Task CleanProgram4Horas(StreamWriter logWriter)
    {
        while (true)
        {

            await CleanProgram(logWriter);
            TimeSpan delayInterval = TimeSpan.FromHours(4); ////----> alterar para 4 horas quando aplicar em prd
            await Task.Delay(delayInterval);
            // Limpar o arquivo de log
            //logWriter.Flush();
        }
    }




    static async Task StartProgram0(StreamWriter logWriter)
    {
        CancellationToken cancellationToken = CancellationToken.None;

        await Task.WhenAll(
            RunProcess(logWriter, "Versao_Regra_DF.exe", cancellationToken),
            RunProcess(logWriter, "Versao_Regra_DP.exe", cancellationToken),
            RunProcess(logWriter, "Divisao_Direta.exe", cancellationToken),
            RunProcess(logWriter, "Back Office DP Transf API Sovos.exe", cancellationToken),
            RunProcess(logWriter, "Front Office - Transf API Sovos.exe", cancellationToken),
            RunProcess(logWriter, "Back Office DF Transf API Sovos.exe", cancellationToken),
            RunProcess(logWriter, "Consulta API Sovos.exe", cancellationToken),
            RunProcess(logWriter, "Consulta API Sovos Duty Free.exe", cancellationToken),
            RunProcess(logWriter, "Transformação_C_One.exe", cancellationToken),
            RunProcess(logWriter, "Grava_API_COne.exe", cancellationToken)
        );
    }
    static async Task CleanProgram(StreamWriter logWriter)
    {
        CancellationToken cancellationToken = CancellationToken.None; // Adicione esta linha para fornecer um token de cancelamento válido     


        logWriter.WriteLine($"Pausa para otimizacao de espaco em disco.");
        Console.WriteLine($"Pausa para otimizacao de espaco em disco.");
        cancellationTokenSource = new CancellationTokenSource();
        await RunProcesses3Async(cancellationTokenSource.Token);
        logWriter.WriteLine($"Otimizacao concluida.");
        Console.WriteLine($"Otimizacao concluida.");
    }



    static CancellationTokenSource cancellationTokenSource;


    static void StopProgram0(StreamWriter logWriter)
    {

        // Forçar o desligamento do Program0
        cancellationTokenSource?.Cancel();

        StopProcess(logWriter, "Versao_Regra_DF.exe");
        StopProcess(logWriter, "Versao_Regra_DP.exe");

        StopProcess(logWriter, "Front Office - Transf API Sovos.exe");
        StopProcess(logWriter, "Back Office DP Transf API Sovos.exe");
        StopProcess(logWriter, "Back Office DF Transf API Sovos.exe");
        StopProcess(logWriter, "Divisao_Direta.exe");
        StopProcess(logWriter, "Back Office DP Transf API Sovos.exe");
        StopProcess(logWriter, "Front Office - Transf API Sovos.exe");
        StopProcess(logWriter, "Consulta API Sovos.exe");
        StopProcess(logWriter, "Consulta API Sovos Duty Free.exe");
        StopProcess(logWriter, "Transformação_C_One.exe");
        StopProcess(logWriter, "Grava_API_COne.exe");
        cancellationTokenSource?.Cancel();


        logWriter.WriteLine($"Program0 stopped.");
    }




    private static async Task<bool> IsDirectoryEmptyAsync(string directoryPath)
    {
        DirectoryInfo directory = new DirectoryInfo(directoryPath);
        FileInfo[] files = directory.GetFiles();
        return files.Length == 0;
    }



    private static async Task DeleteFilesInFolderAsync(string folderPath)
    {
        bool success = false;
        int maxRetries = 3;

        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
            var files = dirInfo.GetFiles();

            await Task.WhenAll(files.Select(async file =>
            {
                int retryCount = 0;
                bool retry = true;

                while (retry && retryCount < maxRetries)
                {
                    try
                    {
                        File.Delete(file.FullName);
                        success = true;
                        retry = false;
                        break;
                    }
                    catch (IOException)
                    {
                        retryCount++;
                        file.Delete();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        retryCount++;
                    }
                    catch
                    {
                        retry = false;
                        break;
                    }
                }
            }));

            // Não é mais necessário excluir os diretórios recursivamente, já que queremos mantê-los

            /// await Task.WhenAll(dirInfo.GetDirectories().Select(dir => DeleteFilesInFolderAsync(dir.FullName)));

            /// dirInfo.Delete();
        }
        catch
        {
        }

        if (!success)
        {
            // Lidar com a falha, se necessário
        }
    }






















    static bool IsFileInUse(string filePath)
    {
        try
        {
            using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // O arquivo não está em uso
                return false;
            }
        }
        catch (IOException)
        {
            // O arquivo está em uso
            return true;
        }
    }



    static void MoveFileToErrorFolder(string filePath)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(erro_file, fileName);
            File.Move(filePath, destinationPath);
            Console.WriteLine($"Moved file to error folder: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving file to error folder: {ex.Message}");
        }
    }





    static async Task StopProcess(StreamWriter logWriter, string processName)
    {
        Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));

        foreach (Process process in processes)
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow(); // Tenta fechar a janela principal do processo
                await Task.Delay(500); // Aguarda um tempo para que o processo possa responder


                process.Close(); // Fecha o processo (encerramento suave)
                await Task.Delay(500); // Aguarda um tempo para que o processo possa encerrar



                process.Kill(); // Caso o encerramento suave não tenha funcionado, faz um encerramento forçado
                process.WaitForExit();

            }

            logWriter.WriteLine($"Process {processName} stopped.");
        }
    }





    static void ScheduleProgram0Shutdown(object state)
    {
        StreamWriter logWriter = (StreamWriter)state;

        // Obter a hora atual no fuso horário do Brasil
        var brTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        var currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, brTimeZone);

        // Calcular a hora de início e término desejada para hoje
        var startTimeToday = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 30, 0);
        var endTimeToday = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 19, 30, 0).AddDays(1);

        // Verificar se a hora atual está após o horário de término de hoje
        if (currentTime >= endTimeToday)
        {
            // Calcular a hora de início e término desejada para o próximo dia
            var startTimeTomorrow = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 30, 0).AddDays(1);
            var endTimeTomorrow = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 19, 30, 0).AddDays(2);

            // Calcular o tempo restante até a hora de início de amanhã
            var timeRemaining = startTimeTomorrow - currentTime;

            // Agendar o desligamento do Program0 para amanhã
            Timer program0ShutdownTimer = null;
            program0ShutdownTimer = new Timer(_ =>
            {
                StopProgram0(logWriter);
                program0ShutdownTimer.Dispose();

                // Agendar o desligamento do Program0 para o próximo dia
                ScheduleProgram0Shutdown(logWriter);
            }, null, timeRemaining, TimeSpan.FromMilliseconds(-1));
        }
        // Verificar se a hora atual está dentro do intervalo desejado para hoje
        else if (currentTime >= startTimeToday && currentTime < endTimeToday)
        {
            // Calcular o tempo restante até a hora de término de hoje
            var timeRemaining = endTimeToday - currentTime;

            // Agendar o desligamento do Program0
            Timer program0ShutdownTimer = null;
            program0ShutdownTimer = new Timer(_ =>
            {
                StopProgram0(logWriter);
                program0ShutdownTimer.Dispose();

                // Agendar o desligamento do Program0 para o próximo dia
                ScheduleProgram0Shutdown(logWriter);
            }, null, timeRemaining, TimeSpan.FromMilliseconds(-1));
        }
    }




}




