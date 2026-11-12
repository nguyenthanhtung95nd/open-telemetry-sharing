# Script thuyết trình: OpenTelemetry & Logging in .NET

---

# PHẦN 1: OPENTELEMETRY

## 1. otel-cover, "OpenTelemetry in .NET"

Chào mọi người. Hôm nay mình sẽ nói về OpenTelemetry trong .NET, cụ thể là Tracing, Metrics, và
cơ chế tự động correlate chúng lại với nhau, những thứ mà logging có cấu trúc như Serilog không
tự cho mình được dù nó đã làm rất tốt việc ghi lại từng sự kiện riêng lẻ. Source code đầy đủ của
cả 2 project demo sẽ được chia sẻ ngay sau buổi nói chuyện, nên mọi người cứ tập trung nghe,
không cần chép code trong lúc này.

**Hỏi:** Cần biết OpenTelemetry trước không? → Không, đi từ khái niệm cơ bản (Observability là
gì) trước khi vào code thật.

---

## 2. otel-agenda, "Nine Questions, in the Order You'd Actually Hit Them"

Đây là 9 chủ đề mình sẽ đi qua, xếp theo đúng thứ tự mà một người thực sự bắt tay vào làm sẽ gặp
phải, từ khái niệm, đến instrument thủ công, Tracing, Metrics, Collector, Logging, Context
Propagation, đi sâu vào Tracing, và cuối cùng là Sampling. Mỗi dòng trong bảng này là một câu hỏi
thực tế, cột bên phải là thứ mọi người sẽ làm được sau khi nghe xong phần đó. Một điểm mình muốn
nhấn mạnh ngay từ đầu: toàn bộ ví dụ trong buổi nói chuyện này lấy từ 2 project thật, đang chạy
được, OrderPipeline, một hệ thống 5 service có RabbitMQ và một Saga, và Bookmarks, một service
đơn không có message broker. Không có ví dụ nào ở đây là code viết riêng cho slide.

**Hỏi:**
- Sao chọn đúng 9 chủ đề này? → Lộ trình chuẩn: khái niệm → instrument → từng signal → hạ tầng
  → context → nâng cao. Thiếu mỗi Logging nền tảng, nằm riêng ở Appendix vì độc lập với OTel.
- 2 project khác gì nhau? → OrderPipeline chứng minh OTel chạy được trong hệ phân tán thật.
  Bookmarks chứng minh ngược lại, không cần kiến trúc phức tạp vẫn áp dụng y hệt.

---

## 3. otel-intro-observability, "Monitoring Tells You Something's Wrong. Observability Tells You Why."

Mình muốn bắt đầu bằng đúng câu hỏi lẽ ra phải hỏi trước tiên: Serilog, CloudWatch Logs
Insights, những công cụ chúng ta đã quen dùng, cho mình biết được gì, và quan trọng hơn, không
cho mình biết được gì? Một dashboard dựng từ Serilog hay CloudWatch cho bạn biết checkout bị
chậm lúc 2 giờ chiều, nhưng đó là vì có ai đó đã nghĩ trước câu hỏi đó và dựng sẵn một biểu đồ
cho nó. Observability là khả năng trả lời một câu hỏi mà không ai nghĩ trước, bằng cách tự đào
sâu vào dữ liệu đã có sẵn, không cần biết trước câu hỏi là gì.

Nhìn vào bảng so sánh này: dịch vụ nào trong 5 service bị chậm, request này có bình thường không
hay là một ngoại lệ, dòng log này thuộc về request nào, với logging thuần túy, cả 3 câu chỉ trả
lời được bằng cách đoán hoặc ngồi grep tay. Với OpenTelemetry, cả 3 câu được trả lời trực tiếp
bằng một Trace, một Histogram đã tính sẵn, hoặc một TraceId gắn tự động. Và để chốt lại:
OpenTelemetry là một chuẩn trung lập về vendor, một API, một SDK, và một wire protocol, để tạo
ra 3 loại telemetry này theo cùng một cách, bất kể sau này backend nào sẽ đọc nó.

**Hỏi:**
- Serilog/CloudWatch vô dụng à? → Không, vẫn cần (slide 17 nói rõ). OTel chỉ mở rộng khả năng
  khám phá câu hỏi chưa biết trước.
- "Vendor-neutral" nghĩa là gì? → Chuẩn mở CNCF, không hãng nào sở hữu. Đổi Jaeger/Datadog/
  Elastic chỉ đổi config Collector, không đổi code (chứng minh ở slide Collector).

---

## 4. otel-intro-signals, "Three Signals, Three Different Questions"

OpenTelemetry chuẩn hóa 3 loại telemetry, và không cái nào thay thế cái nào, mỗi loại trả lời
một câu hỏi mà hai loại còn lại không trả lời được. Trace là một cây Span, trả lời chuyện gì đã
xảy ra, theo thứ tự nào, và thời gian đi đâu, cho đúng một request này. Metric là một con số
được tổng hợp sẵn tại nguồn, trả lời điều này có bình thường không, đang tốt lên hay xấu đi, trên
tất cả request. Log là một sự kiện có timestamp, trả lời code đã nói chính xác điều gì, bằng lời
của chính nó. Còn một khái niệm thứ 4 trên slide này, Baggage, nhưng nó không phải signal, nó là
context dạng key-value đi theo request, Span hay Log nào muốn đọc thì tự đọc lấy, mình sẽ nói kỹ
hơn ở phần Context Propagation.

**Hỏi:** Có Trace rồi cần Metric làm gì? → Giữ mọi Trace để tính percentile rất tốn. Metric tổng
hợp sẵn tại nguồn, rẻ hơn nhiều.

---

## 5. otel-intro-context, "Three Ids Are What Make 'Correlation' Possible"

Đây là phần mình nghĩ là quan trọng nhất của Introduction, vì nó giải thích cơ chế thực sự đứng
sau từ "correlation" mà mọi người hay nghe. Mỗi Span có một TraceId và một SpanId, một Span con
còn ghi lại ParentSpanId của nó. Chỉ cần 3 trường này, cây Trace tự dựng lại được, không cần
database trung tâm, không cần registry nào cả. Và đây là output console thật, lấy từ
Bookmarks.Api, sau một lệnh gọi GET /bookmarks, mọi người nhìn vào: Activity.TraceId,
Activity.SpanId, Activity.DisplayName, Activity.Kind: Server. Đây chính là một Span, viết ra
console, không qua dashboard nào, cách dễ nhất để hiểu Span thực sự là gì trước khi mở bất kỳ UI
phức tạp nào. Và để chốt lại: cùng một TraceId này là thứ một dòng Log mang theo, là thứ một
Metric's exemplar trỏ về, và là thứ bạn dán vào ticket hỗ trợ khách hàng. Đây là giá trị duy
nhất nối cả 3 signal, và mọi Span trong cây, về cùng một request.

**Hỏi:**
- Id sinh theo chuẩn gì? → W3C Trace Context. TraceId 16 byte / SpanId 8 byte, random, tự động
  qua Activity/ActivitySource.
- Trùng TraceId thì sao? → Xác suất gần như bằng 0, 128-bit random.

---

## 6. otel-manual-activitysource, "OpenTelemetry Didn't Add a New API. .NET Already Had One."

Đây là điểm mình thấy nhiều người hiểu lầm nhất: System.Diagnostics.Activity và ActivitySource
đã có sẵn trong .NET từ bản .NET 5, trước khi stack OpenTelemetry này tồn tại. Một Activity
chính là một Span. SDK của OpenTelemetry chỉ là một listener, đăng ký lắng nghe các source mình
đặt tên, rồi export ra ngoài. Mọi người nhìn 2 đoạn code này, y hệt nhau, chỉ khác tên service,
nhưng lấy từ 2 project hoàn toàn không liên quan, Payment.Api, một microservice trong hệ 5
service dùng RabbitMQ, và Bookmarks.Api, một REST API đơn không hề có message broker. Cả 2 đều
chỉ là: public static readonly ActivitySource ActivitySource = new(tên service). Instrumentation
code không hề biết, và không hề quan tâm, nó đang chạy trong hệ nào.

**Hỏi:**
- Activity và Span có phải 1 không? → Đúng, Microsoft gọi Activity, OTel gọi Span, cùng 1 thứ.
- Không cài OTel thì StartActivity() làm gì? → Vẫn tạo object, nhưng không ai listen thì trả
  null ngay, gần như miễn phí.

---

## 7. otel-manual-resource, "A Span Is Meaningless Until You Say Whose It Is"

Một Span tồn tại một mình thì vô nghĩa, nó cần biết dịch vụ nào, phiên bản nào, môi trường nào.
Đó là Resource, tập attribute được đóng dấu lên mọi Span, Metric, Log mà một process phát ra.
Không có nó, một backend chứa telemetry từ 5 service sẽ không phân biệt được cái nào của ai. Đây
là cấu hình thật trong appsettings.json của Bookmarks.Api: ServiceName, ServiceNamespace, và
ResourceAttributes tùy chỉnh như team. Điều mình muốn mọi người để ý: cùng một convention
service.namespace này cho phép 5 service .NET của OrderPipeline và front-end Next.js của nó,
Web.BFF, xuất hiện chung một namespace trong Jaeger, dù một bên là C#, một bên là TypeScript,
đoạn code TypeScript bên dưới, @vercel/otel, cũng set service.namespace: "OrderPipeline" y hệt.

**Hỏi:**
- Set 1 lần hay mỗi Span? → 1 lần lúc khởi động TracerProvider, áp dụng suốt vòng đời process.
- ServiceName khác ServiceNamespace thế nào? → Name là tên 1 service, Namespace là nhóm nhiều
  service liên quan, backend dùng để nhóm, lọc.

---

## 8. otel-manual-libraries, "Automatic Instrumentation Isn't Magic, It's Subscription"

Một Instrumentation Library là code người khác viết sẵn, tự gọi ActivitySource.StartActivity
giúp mình, tại đúng những điểm framework cần. Mình muốn cho mọi người thấy 2 cách khác nhau để
bật instrumentation cho database, cả 2 đều thật. Cách 1, Payment.Api, thêm một NuGet package,
gọi tracing.AddNpgsql(), cần package riêng cho từng service. Cách 2, Bookmarks.Api, không có
package Npgsql riêng nào cả, chỉ cần observability.ActivitySources.Add("Npgsql"), tức là chỉ đặt
tên cái ActivitySource mà driver Npgsql đã tự publish sẵn, driver đã tự tạo Span, mình chỉ cần
đăng ký lắng nghe nó. Dù cách nào, kết quả là cùng một cây 3 Span: GET bookmarks, tự động, Server,
bọc Bookmarks.List, viết tay, Internal, bọc postgresql, tự động, Client, không viết một dòng code
nào tại nơi gọi.

**Hỏi:**
- Sao Npgsql không cần package như AspNetCore? → Cách đặt tên ActivitySource áp dụng cho mọi
  thư viện tự publish, kể cả AspNetCore, AddAspNetCoreInstrumentation() chỉ là extension tiện.
- Biết thư viện nào publish ActivitySource thế nào? → Đọc docs/source tìm new ActivitySource(...).
  README Bookmarks ghi rõ đây là gap đã biết của Shared.Observability.

---

## 9. otel-tracing-configure, "One Builder, Five Decisions"

Cấu hình Tracing nghĩa là trả lời, theo đúng thứ tự, Sampler nào, Instrumentation Library nào,
ActivitySource viết tay nào, có copy Baggage lên Span không, và export đi đâu. Đây là code thật,
trong TracingConfigurator.cs của Shared.Observability, thư viện này được vendor y hệt, không đổi
một dòng, trong cả 2 project. Mọi người nhìn method Configure, 5 dòng, đúng 5 quyết định mình
vừa nói, mọi service gọi builder.AddSharedObservability(...) đều chạy qua chính xác method này,
OrderPipeline.Api và Bookmarks.Api chỉ khác nhau ở options truyền vào, không khác code chạy.

**Hỏi:** Thứ tự có quan trọng không? → Có: Sampler phải set trước Span đầu tiên, Processor phải
add trước Exporter (xem slide 24).

---

## 10. otel-tracing-jaeger-proof, "One Click, Four Services, Zero Manual Correlation Code"

Đây là ảnh chụp thật, từ Jaeger, sau khi mình tự tay bấm Place order trên trang OrderPipeline
Store. @vercel/otel ở trình duyệt, instrumentation có sẵn của Next.js, và
AddAspNetCoreInstrumentation ở cả 2 service .NET, tất cả cùng truyền tay một header
traceparent, hoàn toàn tự động. Mọi người nhìn số liệu trên ảnh: 4 service, 15 Span, 279
mili giây, từ cú click chuột thật ở Web.Browser, qua Web.BFF, Gateway.Api, đến
OrderPipeline.Api, không ai viết code để nối các Span này lại. Nhưng mình muốn nói chậm ở đây:
đây chỉ là phần HTTP, từ Browser đến OrderPipeline.Api. Chuyện gì xảy ra tiếp theo, qua RabbitMQ,
mình sẽ nói ở phần Context Propagation, và nó không sạch như thế này.

**Hỏi:**
- Sao chỉ 4 service không phải 5-6? → Chỉ là đường HTTP. Inventory/Payment xử lý async qua
  RabbitMQ, ngoài trace này (slide 21 mổ xẻ).
- 279ms có bất thường không? → Tổng cả gọi Keycloak lấy token, môi trường local, không phải
  benchmark production.

---

## 11. otel-tracing-tags, "A Tag Turns a Span Into a Decision You Can Query"

Một Span chỉ có tên và duration thì chỉ nói được có gì đó chậm. Một Span có tag nói được đơn
hàng nào, kết quả nào, để mọi người filter, group theo chúng ở backend, chứ không phải đọc từng
cái một. Đây là code thật từ StockReservedHandler.cs của Payment.Api: SetTag(OrderId, ...),
SetTag(Amount, ...), và khi fail thì SetTag(Outcome, Failed). Cùng một pattern này lặp lại trong
Bookmarks.Api, enrich một loại Span khác, sau khi ghi database thành công. Một chi tiết nhỏ mình
muốn chỉ ra, dấu ?. trong activity?.SetTag, quan trọng vì Sampler có thể quyết định không ghi lại
Span này, và lệnh gọi vẫn phải an toàn.

**Hỏi:**
- Tag = Attribute? → Cùng 1 thứ, SetTag là tên method .NET, OTel gọi "Span Attribute".
- Giới hạn số tag? → Mặc định SDK .NET giới hạn 128/span, vượt quá bị drop âm thầm.

---

## 12. otel-metrics-collect, "A Metric Is a Number That's Already Cheap by Export Time"

Đào log để tìm xu hướng nghĩa là quét từng dòng, mỗi lần, tại thời điểm query. Một Meter tổng
hợp sẵn ngay tại nguồn, nên lúc export, backend chỉ lưu vài con số, không phải từng request. Có
3 loại instrument, mình có ví dụ thật cho cả 3: Counter, chỉ tăng, bookmarks.created, Histogram,
phân phối giá trị, tính sẵn p50/p95/p99, bookmarks.operation.duration, ObservableGauge, giá trị
tức thời đọc qua callback, như runtime metrics GC heap size. Điểm hay mình muốn nhấn: một Counter
và một Span có thể bắt đầu cùng lúc, nên không bao giờ lệch nhau, helper ServiceOperation của
Bookmarks.Api bắt đầu cả 2 cùng lúc, ghi Histogram khi Dispose() chạy, xem code: using var
operation = ServiceOperation.Start(...), rồi BookmarksCreatedCounter.Add(1).

**Hỏi:**
- Khi nào Counter, khi nào Histogram? → Counter đếm rời rạc. Histogram cho giá trị liên tục cần
  phân phối (không chỉ trung bình mà cả p95/p99).
- ObservableGauge khác gì? → SDK tự gọi ngược vào callback của bạn, không phải chủ động Add(),
  hợp cho giá trị đọc bất cứ lúc nào.

---

## 13. otel-metrics-prometheus-proof, "Exporting to Prometheus"

Exporter prometheus của Collector biến orders.created thành orders_created_total, convention
đặt tên riêng của Prometheus cho một Counter, rồi serve cho Prometheus scrape. Ảnh chụp thật
này, đường biểu đồ nhảy từ 1 lên 2, chính xác là 2 đơn hàng thật mình đặt cách nhau vài phút khi
chuẩn bị cho buổi nói chuyện này, không phải số liệu giả lập. exported_job chính là Resource
attribute mình nói ở phần Manual Instrumentation, đi theo suốt tới tận đây.

**Hỏi:** Sao tên đổi orders.created → orders_created_total? → Dấu chấm không hợp lệ trong
Prometheus, tự chuyển "_", hậu tố "_total" là convention bắt buộc cho Counter.

---

## 14. otel-collector-what, "One Pipe, Not One Vendor"

CloudWatch Logs và Application Insights trói code instrumentation của mình vào SDK của một
hãng. Với OpenTelemetry, mỗi service chỉ cần biết một địa chỉ, địa chỉ của Collector. Collector
quyết định telemetry đi đâu tiếp, Jaeger, Prometheus, Loki, mà code của service không bao giờ
phải đổi.

**Hỏi:** Collector có bắt buộc không? → Không, slide ngay sau nói rõ khi nào cần, Bookmarks là
ví dụ thật không dùng Collector.

---

## 15. otel-collector-when, "You Don't Always Need One"

Bookmarks không có Collector nào cả. README của chính nó nói thẳng quyết định này, và đây cũng
là quyết định mọi người nên áp dụng cho bất kỳ service nào: một service, một backend hiểu OTLP,
không cần Collector, Bookmarks gửi thẳng tới Elastic Cloud. Backend không hiểu OTLP như
Prometheus hay Loki, cần. Muốn tỏa ra 2 backend trở lên cùng lúc, cần. Muốn một điểm ra, một
credential, cho nhiều service, cần. Với Bookmarks, một Collector sẽ chỉ nhận OTLP rồi chuyển
tiếp chính xác OTLP đó, không đổi gì, không phải là một pipeline, chỉ là một chặng dừng vô
nghĩa. Chỉ thêm Collector khi một trong các dòng ở bảng trở thành đúng với mình, không thêm
trước.

**Hỏi:** Nên thêm sớm phòng khi cần không? → Nếu chắc chắn sẽ scale thì thêm sớm, còn không thì
đừng thêm hạ tầng cho nhu cầu chưa tồn tại.

---

## 16. otel-collector-configure, "Receiver, Processor, Exporter: Nothing Else"

Mọi config Collector đều chỉ có 3 giai đoạn, ghép thành pipeline theo từng signal. Đây là config
thật, đang chạy: receivers.otlp nhận cả gRPC và HTTP, processors gồm memory_limiter, attributes,
tail_sampling, mình sẽ nói kỹ ở phần Sampling, và batch, exporters: debug, otlp ra Jaeger,
prometheus, loki. Mọi service chỉ cần trỏ về cùng một địa chỉ, biến môi trường
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317, cùng một tên biến, mọi service, mọi
signal. Đổi Jaeger sang Elastic APM chỉ là sửa một block exporter trong file YAML này, không
phải redeploy 5 service.

**Hỏi:**
- Processor có thứ tự chạy không? → Có, tuần tự theo khai báo, memory_limiter phải đứng đầu để
  bảo vệ Collector khỏi OOM.
- debug exporter dùng ở production được không? → Không nên, rất tốn, chỉ để debug cấu hình.

---

## 17. otel-logging-role, "OpenTelemetry Doesn't Replace Your Logger"

Một Trace nói chuyện gì xảy ra và thời gian đi đâu. Một Log nói chính xác code đã nói gì, bằng
lời của nó, tại đúng khoảnh khắc đó. Không cái nào làm thay việc của cái kia. Thêm logging bridge
của OpenTelemetry là thêm một đường xuất thứ hai song song với ILogger, không xóa bỏ console
logger mà một platform như CloudWatch vẫn đang đọc. Bookmarks.Api thậm chí có một đường log thứ
hai, cố tình không dùng OpenTelemetry, để chứng minh rõ điểm này: AuditLoggerProvider đóng dấu
Activity.Current?.TraceId lên console thường, bỏ qua cả log-level filtering. TraceId chỉ là một
chuỗi string, bất cứ thứ gì đọc được Activity.Current đều gắn được nó, có hay không có SDK
OpenTelemetry.

**Hỏi:** Nên bỏ Serilog chuyển hẳn OTel logging không? → Không cần, giữ Serilog, thêm bridge nếu
muốn log chảy chung backend với Trace/Metric.

---

## 18. otel-logging-configure, "Configuring and Exporting Logs: Two Separate Calls"

.WithLogging(...) nhận 2 delegate có chủ đích: một cái cấu hình pipeline export, một cái cấu
hình bản thân ILogger hoạt động ra sao. Code thật, delegate đầu bật AddConsoleExporter hoặc
AddOtlpExporter tùy config, delegate sau set IncludeFormattedMessage = true và IncludeScopes =
true. Mình muốn nhấn mạnh chỗ này: nếu thiếu IncludeFormattedMessage, Loki sẽ lưu riêng rẽ
template string và tham số, không bao giờ lưu câu mọi người thực sự đã log, đây là lỗi rất dễ
gặp khi mới setup.

**Hỏi:** Hậu quả cụ thể khi thiếu setting đó? → Backend hiện "Order {OrderId} created" + field
rời OrderId=4, thay vì câu hoàn chỉnh, khó search full-text.

---

## 19. otel-logging-loki-proof, "Exporting to Loki"

Đây là query thật trong Grafana, {job="OrderPipeline/OrderPipeline.Api"}, chụp ngay sau khi đặt
một đơn hàng. Mỗi dòng đã có sẵn traceid, spanid, và đúng nội dung message, "Order 4 created with
1 items totalling 19.99", không cần parse chuỗi để lấy ra các giá trị này, chúng đã là field
riêng. Mọi dòng trong danh sách này chia sẻ chung một traceid với Span đang active lúc nó được
log, đây chính là cùng một Trace mà mọi người thấy dạng timeline trong Jaeger, giờ đọc lại dưới
dạng text.

**Hỏi:** Sao không dùng ELK thay Loki? → Loki nhẹ hơn, index theo label chứ không full-text, rẻ
hơn ở quy mô nhỏ. Về nguyên lý export OTLP logs đi backend nào cũng như nhau.

---

## 20. otel-logging-enrich, "Logs Get the Same Enrichment as Spans, From the Same Source"

Bookmarks đăng ký 2 Processor từ cùng một allow-list các Baggage key, một cho Span, một cho Log.
Không cái nào tự bịa ra dữ liệu mới, cả 2 chỉ copy lại thứ caller đã set sẵn: BaggageTagProcessor
gọi activity.SetTag(...), BaggageLogProcessor gộp vào logRecord.Attributes. Cả 2 đọc cùng một
option AllowedKeyPrefixes, nên một Span và một dòng Log ghi ngay sau đó mang y hệt business
context.

**Hỏi:** Processor chạy mỗi lần log hay 1 lần? → Mỗi lần có telemetry mới (OnEnd mỗi Span/mỗi
LogRecord), không phải lúc khởi động.

---

## 21. otel-context-services, "HTTP Propagates Itself. RabbitMQ Doesn't, by Default."

Đây là phần mình sẽ nói chậm và rõ nhất trong cả buổi, vì đây là phát hiện thật, mình tự kiểm
chứng trực tiếp ngay trước buổi nói chuyện này. Instrumentation Library xử lý HTTP cũng tự xử lý
luôn propagation: mọi HttpClient tự động inject traceparent, mọi request đến ASP.NET Core tự
động đọc lại. Message queue thì không có thư viện như vậy, OrderPipeline phải viết tay
Propagator.Inject trước khi publish, và Propagator.Extract trước khi consume, ở cả 2 đầu.

Và đây là phần quan trọng nhất: mình đã tự kiểm tra trực tiếp lúc chuẩn bị, bằng cách đặt một
đơn hàng thật rồi tra thẳng vào Jaeger API. Dù code Inject/Extract đã viết đúng, một lần checkout
thật vẫn tách ra thành 3 trace riêng biệt, đường HTTP, Browser tới OrderPipeline.Api, liên kết
hoàn hảo, nhưng inventory.order-created process và payment.stock-reserved process đều bắt đầu
như một root trace mới, không có parent nào cả, nghĩa là 2 chặng RabbitMQ, OrderPipeline sang
Inventory, và Inventory sang Payment, đều không liên kết được vào trace gốc. Mọi người nhìn ảnh
này, dòng "Services 1" ở đầu chính là bằng chứng. Bài học không phải là RabbitMQ propagation dễ
làm, bài học là: có code Inject/Extract không đồng nghĩa với việc nó thực sự hoạt động, luôn
luôn kiểm tra Trace thật trong backend của mình trước khi khẳng định nó đúng.

**Hỏi:**
- Lỗi nằm ở đâu, đã fix chưa? → Chưa xác định nguyên nhân gốc chính xác, code có vẻ đúng, mọi
  service đăng ký đúng ActivitySource. Triệu chứng rất rõ, nguyên nhân cần điều tra thêm.
- Demo có "thất bại" không? → Ngược lại, đây là bài học thật nhất: propagation qua queue luôn
  cần viết tay và verify, không tự nhiên đúng như HTTP.
- Cách đảm bảo chắc chắn? → Luôn kiểm tra Jaeger thật sau deploy, hoặc integration test so
  Activity.Current?.TraceId ở 2 đầu publish/consume.

---

## 22. otel-context-baggage, "Baggage: the Same W3C Header, Two Very Different Senders"

Baggage là context dạng key-value, đi trong header baggage chuẩn, tách biệt với traceparent. Ai
đọc nó thì tự quyết định làm gì với nó, đó là lý do nó hoạt động được kể cả khi không có SDK
OpenTelemetry nào ở đầu gửi. Mình có 2 ví dụ hoàn toàn khác nhau, cùng một cơ chế: Bookmarks, một
trình duyệt, không có SDK nào, chỉ set một header fetch thường, app.action=...,app.screen=....
OrderPipeline, service gọi service, set bằng API .NET, Baggage.SetBaggage(OrderPipelineTags
.OrderId, order.Id.ToString()). Dù cách nào, không ai viết code để đọc nó ở đầu kia,
BaggageTagProcessor của Shared.Observability tự copy các key được allow-list lên mọi Span, tự
động, ở cả 2 project.

**Hỏi:**
- Baggage khác Trace Context ở đâu? → traceparent chỉ mang Id/flags. Baggage mang business data
  tùy ý, không tự tạo Span.
- Baggage qua RabbitMQ có propagate không (dựa slide trước)? → Dùng chung cơ chế Inject/Extract,
  nên gãy theo đúng 2 chặng đó, chỉ chắc chắn sống sót qua HTTP.

---

## 23. otel-context-risk, "Baggage Goes Everywhere the Request Goes, Including Out"

Baggage bám theo mọi lệnh gọi ra ngoài sau khi được set, không chỉ giữa các service của mình.
Nếu request đó sau này gọi một API bên thứ 3, Baggage đó cũng đi theo tới đó. Đây là input không
đáng tin, theo thiết kế, vì ai đọc được request cũng set được nó. Cả 2 project phòng vệ giống
nhau: allow-list theo prefix, áp dụng trước khi copy lên bất kỳ Span hay Log nào, kèm giới hạn
cứng, MaxEntries = 16, MaxValueLength = 256. Không cấu hình prefix nào thì Processor copy không
gì cả, đó là mặc định an toàn. Bookmarks chỉ cho phép app.

**Hỏi:**
- Lỡ set PII rồi gỡ sao? → Không gỡ được sau khi export, telemetry lưu rồi coi như đã rò rỉ.
  Phòng ngừa duy nhất: không bao giờ set PII từ đầu.
- Allow-list đủ an toàn chưa? → Chỉ ngăn lọt dữ liệu không mong muốn, không mã hóa trên dây, TLS
  là lớp bảo vệ khác.

---

## 24. otel-deep-span-processor, "A Span Processor Sees Every Span, Before Export"

Một Span Processor là một hook mà SDK gọi mỗi khi một Span bắt đầu và kết thúc, trước khi
Exporter nhìn thấy nó. Đây chính là cách Baggage enrichment ở phần Context Propagation được hiện
thực, không phải trường hợp đặc biệt, chỉ là một Processor mình tự viết được. Mọi người nhìn
toàn bộ BaggageTagProcessor.cs: kế thừa BaseProcessor<Activity>, override OnEnd, gọi
CopyAllowedBaggage rồi base.OnEnd. Đăng ký y hệt cách đăng ký một exporter, và thứ tự quan
trọng, một Processor add sau Exporter thì add quá trễ để thay đổi thứ được export.

**Hỏi:**
- Processor làm gì khác được? → Đổi tên Span, lọc Span trước export, redact dữ liệu nhạy cảm,
  tính field mới từ tag có sẵn.
- BaseProcessor<Activity> vs <LogRecord>? → Chỉ khác kiểu generic, 1 cho Span 1 cho Log, cùng
  pattern.

---

## 25. otel-deep-manual-spans, "A Failure Is a Status, Not Just a Log Line"

Một exception không bắt được không nên biến mất khỏi Trace trong im lặng. RabbitMqConsumer đánh
dấu Span, ghi exception như một Span event, rồi mới log và đẩy message vào dead-letter:
SetStatus(ActivityStatusCode.Error, ex.Message), AddException(ex). Đây cũng chính xác là thứ
policy keep-errors của tail_sampling kiểm tra, status_code của Span, không gọi SetStatus(Error),
policy đó không có gì để khớp. Về Span Links, nối 2 Span không phải quan hệ cha con, ví dụ một
Span batch job trỏ về từng đơn hàng nó xử lý, mình nói thẳng là cả 2 project không cần đến nó,
nên đây không có trích dẫn thật, chỉ là hình dạng API chuẩn: StartActivity(name, kind, links:
[...]).

**Hỏi:**
- Sao thừa nhận Span Links không có ví dụ thật? → Cam kết chỉ dùng code thật cả buổi, honest
  quan trọng hơn vẻ đầy đủ.
- SetStatus(Error) tự làm gì khác (retry)? → Không, chỉ là metadata. Retry/dead-letter là logic
  riêng (BasicNack requeue:false).

---

## 26. otel-sampling-what, "Keeping Every Trace Is a Cost Decision, Not a Default"

Giữ mọi Span trong Jaeger, mọi dòng log trong Loki, tốn storage, và trên backend trả phí thì tốn
cả tiền. Sampling là quyết định có chủ ý giữ lại một phần Trace, vẫn đủ tín hiệu để trả lời các
câu hỏi buổi nói chuyện này đã đặt ra từ đầu. Đây không phải lý thuyết suông: Collector của chính
stack này đang chạy một policy tail_sampling, giữ mọi lỗi nhưng chỉ giữ một nửa những gì còn
lại, đó chính xác là lý do vài ảnh chụp màn hình trước đó trong buổi nói chuyện này phải thử lại
2 lần mới chụp được.

**Hỏi:** "Thử 2 lần" nghĩa là gì? → Lần đặt đơn đầu không hiện trong Jaeger API vì Tail Sampling
có 50% khả năng loại bỏ (không lỗi), phải đặt đơn khác mới chụp được.

---

## 27. otel-sampling-head-vs-tail, "Head Decides Blind. Tail Decides Having Seen Everything."

Head Sampling quyết định ngay tại Span đầu tiên, trước khi ai biết Trace này có lỗi hay không,
rẻ, vì cái gì bị loại sau đó không cần export nữa. Code thật trong
ObservabilitySamplerFactory.cs: ParentBasedTraceIdRatio map sang ParentBasedSampler(new
TraceIdRatioBasedSampler(ratio)). Tail Sampling quyết định sau khi toàn bộ Trace đã về tới, đợi
hết decision_wait, có thể giữ mọi lỗi, vì lúc đó đã biết Trace nào có lỗi. Config thật, đang
chạy: decision_wait: 10s, policy keep-errors, status_code ERROR, và sample-the-rest, 50%
probabilistic. Và đây là điểm mình muốn nói thật chậm: chúng tương tác theo đúng một chiều,
không bao giờ ngược lại, Head chạy trước, nếu Head loại một Trace, Trace đó không bao giờ tới
được Collector, nên Tail không bao giờ có cơ hội giữ nó lại, dù nó có chứa lỗi đi nữa.

**Hỏi:**
- Nên dùng Head hay Tail? → Cả hai, bổ sung nhau: Head giảm tải từ service, Tail ở Collector đảm
  bảo không mất lỗi thật (miễn Head không loại trước).
- decision_wait 10s nghĩa là gì? → Collector buffer Span của 1 TraceId 10 giây, cũng là lý do
  Tail tốn RAM hơn Head.

---

## 28. otel-sampling-custom, "A Custom Sampler Is a Switch Statement, Not a New Concept"

SDK chỉ cần một object implement Sampler. Shared.Observability không viết một thuật toán
sampling mới, nó chỉ chọn giữa các Sampler có sẵn của SDK, từ một giá trị config, đúng hình dạng
mà đa số service thực tế cần. Mọi người đọc toàn bộ ObservabilitySamplerFactory.cs: một switch
expression, map SamplingMode sang AlwaysOnSampler, TraceIdRatioBasedSampler, hoặc trả về null
cho trường hợp Default, và đây là điểm hay, trả null nghĩa là đừng gọi SetSampler luôn, để biến
môi trường chuẩn OTEL_TRACES_SAMPLER vẫn hoạt động cho ai set nó, thay vì bị âm thầm ghi đè.

**Hỏi:**
- Viết Sampler tự chế hoàn toàn thì sao? → Implement Sampler, override ShouldSample(...).
  Factory này chỉ là ví dụ đủ dùng, không phải giới hạn API.
- Sao không cần Sampler phức tạp hơn? → Nhu cầu demo chỉ cần tỷ lệ cố định/giữ hết/không giữ,
  phức tạp hơn thường để Tail Sampling xử lý.

---

## 29. otel-recap, "What Changes When You Add This"

Mình muốn tổng kết lại: Serilog, CloudWatch, App Insights cho bạn một dòng log mỗi sự kiện,
thêm OpenTelemetry cho bạn một Trace nối mọi sự kiện của một request, qua mọi service. Dashboard
chỉ trả lời câu hỏi biết trước, thêm Metric và Trace mà bạn tự khám phá được câu hỏi chưa ai
hỏi. SDK của một hãng trong code, thêm một địa chỉ Collector, backend trở thành một dòng config.
Correlation id nối tay, thêm TraceId và Baggage tự động đi theo request. Và câu cuối cùng mình
muốn dừng lại vài giây: mọi khẳng định trong buổi nói chuyện này đều được kiểm chứng trên 2
project thật đang chạy, kể cả phần không đứng vững khi soi kỹ lần 2, 2 chặng RabbitMQ của Saga
checkout. Đó chính là điều đáng nhớ nhất, cái này chỉ có giá trị nếu bạn liên tục kiểm tra lại nó.

**Hỏi:** Chọn 1 điều bắt đầu ngay tuần sau? → AddOpenTelemetry().WithTracing(t =>
t.AddAspNetCoreInstrumentation()) trong 1 service, nhìn Trace đầu tiên xuất hiện.

---

# PHẦN 2: APPENDIX, LOGGING IN .NET

*(Độc lập với phần OpenTelemetry, có thể bỏ qua nếu hết giờ.)*

## 30. logging-cover, "Logging in .NET"

Phần tiếp theo là appendix, nền tảng logging trong .NET, từ cơ bản ILogger đến Serilog,
performance, và monitoring, tất cả trên cùng một project, OrderPipeline.Api, để mọi người thấy
nó áp dụng vào thực tế như thế nào, không phải ví dụ rời rạc.

**Hỏi:** Liên quan gì OpenTelemetry vừa nói? → Đứng độc lập, cần thiết dù có OTel hay không
(slide 17). Slide 45 nối lại 2 phần.

---

## 31. logging-agenda, "The Foundation, One Topic at a Time"

8 chủ đề mình sẽ đi qua: Logging là gì, ILogger hiện đại, các package logging, Provider là gì,
format message, Serilog là gì, tại sao performance quan trọng, và Monitoring là gì, đi từ khái
niệm cơ bản nhất tới công cụ thực tế đang dùng trong OrderPipeline.Api.

---

## 32. logging-fundamentals, "Four Building Blocks, Six Levels"

4 khối nền tảng: ILogger, cái bạn gọi để ghi message, Category, phần nào của app, mặc định là
tên type đầy đủ, Level, thang mức độ nghiêm trọng để lọc, Provider, log đi đâu, Console,
Application Insights. 6 level, từ thấp đến cao: Trace, Debug, Information, Warning, Error,
Critical, mỗi level là một ngưỡng lọc. Lưu ý, LogLevel.None chỉ dùng để tắt logging trong config,
nó không phải là level thứ 7 để log vào.

**Hỏi:** Category đổi được không? → Mặc định tên type đầy đủ, nhưng tạo được category tùy chỉnh
qua ILoggerFactory.CreateLogger("tên").

---

## 33. logging-writing-good-logs, "DI Gives You ILogger, Named Placeholders Make It Queryable"

Constructor injection cho bạn ILogger<OrdersController>, Host tự dựng ILoggerFactory một lần
lúc khởi động, class của mình chỉ cần xin nó. Điểm quan trọng nhất mình muốn nhấn: không dùng
string interpolation, $"Order {orderId} created", đó là một chuỗi phẳng, không còn gì để filter
hay group theo. Thay vào đó dùng named placeholder, logger.LogInformation("Order {OrderId}
created", orderId), được lưu thành field riêng OrderId = 1023, query được.

**Hỏi:** Compiler cảnh báo nếu dùng sai không? → Có, Roslyn CA2254 cảnh báo string interpolation
trực tiếp trong message của logger.

---

## 34. logging-category-eventid, "Category and Event ID"

Category đi theo namespace, ILogger<OrdersController> tự động trở thành category
OrderPipeline.Api.Controllers.OrdersController. Event ID đặt tên cho một loại sự kiện, độc lập
với level và category, public const int OrderCreated = 2001, đây là hằng số có tên, không phải
con số ma thuật, mỗi loại sự kiện có đúng một Event ID.

**Hỏi:** Event ID dùng để làm gì ngoài đặt tên? → Query/Alert theo đúng 1 loại sự kiện, bất kể
message text đổi sau (xem slide Alerts).

---

## 35. logging-host-and-filtering, "Configuring Logging: the Host, and Its Filters"

Cả 2 sống chung một lớp cấu hình, Host quyết định logging khởi động ra sao, filter quyết định
mỗi provider thực sự ghi gì. Generic Host, Host.CreateDefaultBuilder, sở hữu configuration, DI,
và graceful shutdown, ASP.NET Core tự nó cũng là một Generic Host. Filter theo category, ví dụ
"Console": { "LogLevel": { "Microsoft.AspNetCore": "Information" } }, khi các rule chồng lên
nhau, mình muốn nhấn mạnh, category cụ thể hơn thắng, không phải code thắng config, không phải
cái đăng ký sau cùng thắng.

**Hỏi:** "Cụ thể hơn" ví dụ cụ thể là sao? → Microsoft.AspNetCore cụ thể hơn Microsoft, category
Microsoft.AspNetCore.Routing theo rule của Microsoft.AspNetCore.

---

## 36. logging-providers, "A Provider Is Two Methods, Application Insights Plugs Into Either"

Một Provider chỉ cần 2 method, ILoggerProvider.CreateLogger(string): ILogger, và
ILogger.Log<TState>(...). Mọi lệnh gọi LogInformation hay LogWarning đều chảy xuống đúng
Log<TState> này ở dưới. Application Insights có sẵn một Provider implement đúng interface này,
chỉ khác cách đăng ký tùy theo host, ASP.NET Core dùng builder.Logging.AddApplicationInsights(...),
console app không có DI container sẵn, phải tự dựng bằng tay và tự flush trước khi thoát.

**Hỏi:** Viết Provider tùy biến có khó không? → Lý thuyết 2 method, thực tế thêm IDisposable. So
Serilog Sink (1 method, slide 41) thì phức tạp hơn.

---

## 37. logging-timing-and-scopes, "Timing and Context, Without Repeating Yourself"

Đo thời gian bằng IDisposable, không phải gọi Stop(), using var timer =
logger.BeginTimedOperation(...), Dispose() luôn chạy, kể cả khi có exception, không có Stop()
nào để quên gọi. Scope mang context, using (logger.BeginScope(new Dictionary<string, object>
{ ["OrderId"] = orderId })), mọi log bên trong block này tự động mang OrderId, không cần lặp lại
tham số ở từng lệnh gọi. Về runtime level, ASP.NET Core tự theo dõi appsettings.json, sửa level
lúc đang chạy, lưu file, áp dụng ngay, không cần restart, appsettings.Development.json ghi đè
file gốc khi chạy local.

**Hỏi:** BeginScope hoạt động với mọi Provider không? → Tùy Provider có đọc Scope, Console mặc
định có (bật IncludeScopes).

---

## 38. logging-serilog-sink, "Sink Is Serilog's Word for Provider"

2 cách tích hợp Serilog, khác biệt rất quan trọng, mọi người để ý: UseSerilog(), giữ nguyên
ILogger<T>, code hiện tại, OrdersController gọi _logger.LogInformation(...), không đổi gì cả.
Cách 2, AddSingleton(logger), dùng thẳng Serilog.ILogger, mọi nơi gọi log phải đổi sang
.Information(), .Warning()... Với codebase có sẵn như OrderPipeline.Api, UseSerilog() là lựa
chọn hiển nhiên, không phải sửa lại hàng trăm chỗ gọi log.

**Hỏi:** Khi nào chọn cách 2? → Dự án mới hoàn toàn, muốn API gốc Serilog, không có code cũ cần
tương thích ngược.

---

## 39. logging-serilog-config, "Configuration and Enrichment"

Serilog có section config riêng, "Serilog", tách biệt với section "Logging" chuẩn của .NET,
không share với nhau, MinimumLevel, WriteTo, Enrich, Properties. 3 loại enrichment khác nhau,
mọi người phân biệt: FromLogContext, đọc property được push vào code lúc runtime,
WithMachineName hay WithThreadId, tính lại mỗi lần log, Properties, một giá trị cố định suốt
vòng đời app.

**Hỏi:** Sao section riêng? → Serilog là thư viện bên thứ 3, model config riêng không khớp
category/level chuẩn Microsoft.

---

## 40. logging-masking, "Destructuring and Masking"

{@Order} với dấu @ thực sự destructure object thành các field con, thay vì gọi .ToString(). Cần
bật .Destructure.UsingAttributes() để các attribute như [LogMasked] có tác dụng. Ví dụ,
"an.nguyen@example.com" qua [LogMasked(ShowFirst = 3, PreserveLength = true)] thành
"an.******************", giữ 3 ký tự đầu, che phần còn lại, giữ nguyên độ dài gốc.

**Hỏi:** Quên bật UsingAttributes thì sao? → [LogMasked] bị bỏ qua hoàn toàn, email gốc lộ ra,
lỗi bảo mật dễ mắc, cần test riêng.

---

## 41. logging-custom-sink, "A Custom Sink Is One Method"

So với Provider chuẩn, 2 method, ILoggerProvider và ILogger, một Sink tùy biến của Serilog chỉ
cần đúng 1 method, ILogEventSink.Emit(LogEvent logEvent). Ví dụ OrderConsoleSink, lấy message đã
render, in ra kèm timestamp.

**Hỏi:** Sao Serilog đơn giản hơn hẳn? → Tách rõ "ghi sự kiện" (Sink) khỏi "quản lý vòng đời"
(trong UseSerilog()), ILoggerProvider chuẩn gộp cả 2.

---

## 42. logging-performance, "The Real Cost, and the Fix"

3 chi phí thật, xảy ra ở mọi lệnh gọi log thường: parse message template, box tham số kiểu
value-type thành object[], cấp phát một mảng mới. Với log chạy ở tần suất cao, cộng dồn lại đáng
kể. Giải pháp, [LoggerMessage] source generator, [LoggerMessage(EventId = 2001, Level = ...,
Message = "Order {OrderId} for {CustomerName}")] trên một partial void method, compiler tự sinh
code tối ưu, không parse template lúc runtime nữa. Điểm hay, generator match tham số theo tên,
không theo vị trí.

**Hỏi:** Lúc nào nên dùng LoggerMessage? → Ưu tiên hot path, tần suất cao. Log hiếm khi chạy thì
không cần.

---

## 43. logging-alerts, "Error-Based vs. Query-Based Alerts"

Alert dựa trên lỗi, Exceptions, Failed Requests, dễ setup, nhưng mù hoàn toàn với bất cứ thứ gì
không ném exception. Alert dựa trên query, KQL, query tùy ý theo bất kỳ field nào, category,
EventId, property có cấu trúc, bắt được spike không bao giờ ném exception. Ví dụ bảng này, trong
5 phút bình thường có 3 lần OrderNotFound, Warning, lúc có sự cố thì 500 lần, nhưng cả 2 trường
hợp đều 0 exception. Alert dựa trên lỗi sẽ không bao giờ kích hoạt ở đây, chỉ có query theo
EventId mới bắt được spike này.

**Hỏi:** Nên bỏ error-based không? → Không, vẫn nhanh/đơn giản cho lỗi rõ ràng, nên có cả hai.

---

## 44. logging-project, "OrderPipeline.Api: Everything, Applied"

Tổng kết toàn bộ Appendix Logging trên đúng một project thật: OrdersController, Debug logs, rồi
OrderService, Info/Warn cộng timing, rồi OrderRepository, try/catch cộng LogError, rồi
PostgreSQL, EF Core noise đã lọc bớt. Và các thứ cross-cutting bọc qua mọi lớp: Serilog
pipeline, OrderAuditLoggerProvider, [LoggerMessage].

---

## 45. logging-otel-bridge, "No Microservices? OpenTelemetry Still Applies"

Đây là slide mình dùng để nối lại 2 phần của buổi nói chuyện. Serilog là nền tảng, structured,
fast logging trong một process. Mọi thứ đã nói về Tracing, Metrics, correlation ở phần trước
hoạt động y hệt trong một project đơn, không cần service thứ 2 nào. Một Span quanh một thao tác
nội bộ chậm, một Counter hay Histogram, log gắn TraceId hữu ích ngay cả trong một process khi có
nhiều background task chạy song song, export vendor-neutral từ đầu, nếu sau này project lớn
thành nhiều service, instrumentation không cần đổi gì. Và đây là câu mình muốn chốt cho cả buổi,
nói chậm: đúng AddOpenTelemetry().WithTracing(...) xuyên suốt buổi nói chuyện này là toàn bộ
setup, Microservices chưa bao giờ là điều kiện bắt buộc.

**Hỏi:** Bookmarks liên hệ gì slide này? → Dù viết trước khi Bookmarks tồn tại nên không nhắc
tên, Bookmarks chính là ví dụ thực tế cho đúng luận điểm này.

---

## 46. logging-closing, "Thank You"

Cảm ơn mọi người đã theo dõi. Source code của cả 2 project, OrderPipeline và Bookmarks, sẽ được
chia sẻ ngay sau đây. Rất sẵn lòng trả lời thêm câu hỏi.

---

# FAQ TỔNG HỢP, câu hỏi khó, không gắn với slide cụ thể

**Sao chọn Jaeger/Prometheus/Loki/Grafana (OSS) thay vì Datadog/New Relic?**
Code không khác biệt, chuẩn OTel, chỉ đổi config exporter Collector. OSS chọn cho demo vì miễn
phí, docker compose up local. Bookmarks chứng minh ngược: Elastic Cloud managed chỉ đổi 3 dòng
config.

**Sao có 2 project demo thay vì 1?**
OrderPipeline chứng minh OTel chạy hệ phân tán thật, cũng là nơi phát hiện lỗi propagation
RabbitMQ. Bookmarks chứng minh không cần kiến trúc phức tạp, và verify Shared.Observability
portable thật (copy nguyên vẹn sang .NET 10 khác, không sửa gì).

**Shared.Observability có public/dùng được project khác không?**
Thư viện nội bộ, viết riêng cho 2 demo này, không phải SDK, không nhằm phát hành rộng. Nhưng
pattern tổ chức (1 method đăng ký, Internal/ chia Tracing/Metrics/Logging) dùng lại được.

**Chi phí vận hành thực tế (CPU, bộ nhớ, độ trễ)?**
Không có benchmark chính thức (môi trường local). Về nguyên lý, instrumentation tự động tốn rất
ít overhead, batch exporter giảm tải network. Chi phí lớn hơn thường từ giữ quá nhiều Trace ở
backend, đó là lý do Sampling tồn tại.

**Team chưa dùng OTel, bắt đầu từ đâu?**
Thêm AddOpenTelemetry().WithTracing(t=>t.AddAspNetCoreInstrumentation()) cho 1 service, xem
Trace đầu tiên. Rồi Resource attributes. Rồi Metrics. Collector chỉ khi thực sự cần. Context
Propagation qua queue và Sampling sau cùng, khi đã ổn định.
