using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ContentServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // 제공할 파일 폴더 하나를 정합니다. Unity 프로젝트 전체를 지정하지 않습니다.
            // --FilesRoot 인수가 없으면 ??의 오른쪽인 Content 폴더를 사용합니다.
            string folder = builder.Configuration["FilesRoot"] ?? "Content";
            // 상대 경로라면 서버 프로젝트의 콘텐츠 루트를 기준으로 절대 경로를 구합니다.
            string root = Path.GetFullPath(folder, builder.Environment.ContentRootPath);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException("제공할 폴더가 없습니다: " + root);
            }

            // --Urls 인수가 없으면 수업 기준 주소를 사용합니다.
            string url = builder.Configuration["Urls"] ?? "https://localhost:5443";

            // using 선언으로 Main이 끝날 때 파일 제공자가 사용한 자원을 정리합니다.
            using PhysicalFileProvider files = new PhysicalFileProvider(root);

            // 수업에서 쓰는 형식만 제공합니다. .cs, .csproj 등은 제공하지 않습니다.
            FileExtensionContentTypeProvider types = new FileExtensionContentTypeProvider();
            types.Mappings.Clear();
            // application은 프로그램이 처리할 데이터, octet-stream은 8비트 단위로 이어진 일반 바이너리 데이터를 뜻합니다.
            types.Mappings[".bundle"] = "application/octet-stream";
            types.Mappings[".hash"] = "text/plain";
            types.Mappings[".json"] = "application/json";
            // Web 빌드의 Addressables 카탈로그는 바이너리(.bin)로 생성됩니다.
            types.Mappings[".bin"] = "application/octet-stream";
            types.Mappings[".html"] = "text/html";
            types.Mappings[".js"] = "application/javascript";
            types.Mappings[".css"] = "text/css";
            types.Mappings[".wasm"] = "application/wasm";
            types.Mappings[".data"] = "application/octet-stream";
            types.Mappings[".png"] = "image/png";
            types.Mappings[".ico"] = "image/x-icon";
            types.Mappings[".svg"] = "image/svg+xml";
            // DAY 15 Web 압축 비교용 형식입니다.
            types.Mappings[".gz"] = "application/octet-stream";
            types.Mappings[".br"] = "application/octet-stream";
            // Decompression Fallback 파일은 아래 gzip·br 처리에서 제외하고 Unity의 해제 코드가 읽게 합니다.
            types.Mappings[".unityweb"] = "application/octet-stream";

            WebApplication app = builder.Build();

            // 요청마다 메서드·경로·상태 코드·소요 시간을 출력합니다.
            // 번들·카탈로그 요청 여부와 404를 서버 쪽에서 확인하는 실습 기록용입니다.
            app.Use(async (context, next) =>
            {
                Stopwatch watch = Stopwatch.StartNew();
                await next(context);
                watch.Stop();
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] {context.Request.Method} {context.Request.Path} " +
                    $"-> {context.Response.StatusCode} ({watch.ElapsedMilliseconds} ms)");
            });

            // Web 빌드의 index.html도 같은 서버로 열 수 있게 합니다.
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            // 기본 문서 경로를 먼저 고른 뒤, 실제 파일과 확장자에 맞는 응답 형식을 제공합니다.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = types,
                OnPrepareResponse = context =>
                {
                    // no-cache는 받은 응답을 재사용하기 전에 서버에 확인하라는 뜻입니다. Unity 번들 캐시 삭제는 아닙니다.
                    context.Context.Response.Headers.CacheControl = "no-cache";

                    string extension = Path.GetExtension(context.File.Name);
                    if (extension == ".gz" || extension == ".br")
                    {
                        // 마지막 확장자만 제거합니다. game.wasm.gz라면 game.wasm으로 원래 파일 형식을 찾습니다.
                        string originalName = Path.GetFileNameWithoutExtension(context.File.Name);
                        if (types.TryGetContentType(originalName, out string? contentType))
                        {
                            // 압축된 파일이어도 Content-Type은 원래 데이터 형식(application/wasm 등)으로 보냅니다.
                            context.Context.Response.ContentType = contentType;
                        }

                        // 이미 압축된 파일임을 브라우저에 알려 자동 해제하게 합니다. 서버에서 다시 압축하지 않습니다.
                        context.Context.Response.Headers.ContentEncoding = extension == ".gz" ? "gzip" : "br";
                    }
                }
            });

            app.MapGet("/health", () => Results.Ok(new { status = "ok", root }));

            Console.WriteLine("파일 제공 폴더: " + root);
            Console.WriteLine("서버 주소: " + url);
            // 네트워크 과정에서 준비한 localhost 개발 인증서를 사용합니다.
            app.Run(url);
        }
    }
}
