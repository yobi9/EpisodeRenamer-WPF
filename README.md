# مغير أسماء الحلقات الاحترافي ULTRA

# EpisodeRenamer ULTRA

![C#](https://img.shields.io/badge/C%23-12-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-.NET_8-512BD4)
![License](https://img.shields.io/badge/License-MIT-green)
![Platform](https://img.shields.io/badge/Platform-Windows_x86-blue)

أداة احترافية لإعادة تسمية ملفات الحلقات بشكل تلقائي وذكي، مصممة خصيصاً للمستخدم العربي.

A professional tool for automatically renaming episode files, designed specifically for Arabic-speaking users.

---

## المميزات | Features

- **إعادة تسمية تلقائية ذكية** — يتعرف على أرقام الحلقات ويسمي الملفات بصيغ متنوعة
- **8 أنماط تسمية** — S01E01, S01.E01, Season 01 Episode 01, EP 001, #01, الموسم 01 - الحلقة 01, S1E1, EP01
- **واجهة عربية كاملة** — واجهة من اليمين لليسار (RTL) مع دعم كامل للعربية
- **console م.dark** — شاشة متابعة بلون أخضر على أسود تشبه الـ terminal
- **معاينة مباشرة** — معاينة فورية قبل إعادة التسمية مع تحديث تلقائي
- **تتبع الحلقات المفقودة** — يكتشف الفجوات في تسلسل الحلقات ويحفظ تقريراً
- **撤销 (Undo)** — إمكانية التراجع عن آخر عملية إعادة تسمية
- **تصدير تقارير** — تصدير تقارير المعاينة وإعادة التسمية كملفات نصية
- **سحب وإفلات** — دعم السحب مباشرة إلى النافذة
- **تنقية العلامات** — إزالة علامات المصدر مثل BluRay, WEB-DL, 1080p, مدبلج, مترجم
- **حذف Kashida** — تنظيف النصوص العربية من التشكيل الزائد
- **تعرّف تلقائي على اسم المسلسل** — يستخرج اسم المسلسل من اسم المجلد
- **إعدادات مستمرة** — حفظ آخر إعدادات مستخدمة تلقائياً

## المتطلبات | Requirements

- Windows 7+ (x86 or x64)
- .NET 8.0 Desktop Runtime ([تحميل](https://dotnet.microsoft.com/download/dotnet/8.0))

## التثبيت | Installation

### تحميل الإصدار الجاهز (Binary Release)

1. اذهب إلى [Releases](../../releases)
2. حمّل أحدث إصدار `EpisodeRenamer.App.exe`
3. شغّل الملف مباشرة (لا يحتاج تثبيت)

### البناء من المصدر (Build from Source)

```bash
# استنساخ المستودع
git clone https://github.com/yobi9/EpisodeRenamer-WPF.git
cd EpisodeRenamer/EpisodeRenamer-WPF

# بناء المشروع
dotnet build

# تشغيل الاختبارات
dotnet test

# نشر إصدار قابل للتشغيل
dotnet publish src/EpisodeRenamer.App/EpisodeRenamer.App.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish-x86
```

## الاستخدام | Usage

1. **اختر مجلد الحلقات** — اضغط "تصفح" أو اسحب المجلد إلى النافذة
2. **أدخل اسم المسلسل** — يُكتشف تلقائياً من اسم المجلد أو اضغط "اقتراح"
3. **اختر نمط التسمية** — من القائمة المنسدلة (8 أنماط متاحة)
4. **معاينة** — اضغط "معاينة" لمراجعة التغييرات قبل التنفيذ
5. **إعادة التسمية** — اضغط "إعادة تسمية" لتطبيق التغييرات

## نمط التسمية | Naming Styles

| # | النمط | المثال |
|---|-------|--------|
| 0 | S01E01 (نمط بليكس القياسي) | `My Show-S01E001.mkv` |
| 1 | S01.E01 (نمط التورنت) | `My Show-S01.E01.mkv` |
| 2 | Season 01 Episode 01 (وصفي) | `My Show-Season 01 Episode 01.mkv` |
| 3 | EP 001 (أنمي - 3 أرقام) | `My Show-EP001.mkv` |
| 4 | #01 (أنمي مختصر) | `My Show-#01.mkv` |
| 5 | الموسم 01 - الحلقة 01 (عربي) | `My Show-الموسم 01 الحلقة 01.mkv` |
| 6 | S1E1 (بدون أصفار) | `My Show-S1E1.mkv` |
| 7 | EP01 (أنمي - رقمين) | `My Show-EP01.mkv` |

## هيكل المشروع | Project Structure

```
EpisodeRenamer-WPF/
├── src/EpisodeRenamer.App/
│   ├── Core/                    # المنطق الأساسي
│   │   ├── TextNormalizer.cs    # تنظيف وتطبيع النصوص
│   │   ├── NameFormatter.cs     # تنسيق أسماء الحلقات
│   │   ├── SeasonResolver.cs    # تحليل أسماء مواسم المجلدات
│   │   ├── EpisodeNameGenerator.cs # توليد الأسماء الجديدة
│   │   ├── FileDiscovery.cs     # اكتشاف ملفات الفيديو
│   │   ├── FileProcessingEngine.cs # محرك المعالجة الرئيسي
│   │   ├── GapAnalyzer.cs       # تحليل الحلقات المفقودة
│   │   ├── MissingEpisodesWriter.cs # كتابة تقرير الحلقات المفقودة
│   │   ├── SettingsManager.cs   # إدارة الإعدادات
│   │   ├── UndoLogManager.cs    # إدارة سجل التراجع
│   │   ├── ReportExporter.cs    # تصدير التقارير
│   │   ├── AppSettings.cs       # نموذج الإعدادات
│   │   ├── RenameEntry.cs       # نموذج سجل إعادة التسمية
│   │   ├── RomanNumeralConverter.cs # تحويل الأرقام الرومانية
│   │   ├── ShowNameDetector.cs  # كشف اسم المسلسل
│   │   └── FileProcessingTypes.cs  # الأنواع المشتركة
│   ├── UI/                      # مكونات الواجهة
│   │   ├── NativeFolderPicker.cs # منتقي المجلدات الأصلي
│   │   ├── ShowNameDialog.cs    # نافذة اسم المسلسل
│   │   └── DialogService.cs     # خدمة الحوارات
│   ├── MainWindow.xaml          # تصميم الواجهة
│   ├── MainWindow.xaml.cs       # كود الواجهة
│   ├── App.xaml                 # تطبيق WPF
│   └── AssemblyInfo.cs
├── tests/EpisodeRenamer.Tests/  # اختبارات الوحدة والتكامل
├── publish-x86/                 # الإصدار المنشور
└── PLAN.md                      # خطة التطوير
```

## الاختبارات | Testing

```bash
dotnet test
```

- **166 اختبار** — اختبارات وحدة + اختبارات تكامل + اختبارات واجهة
- **10 فئات اختبار** — تغطي جميع مكونات النظام

## الترخيص | License

MIT License - مفتوح المصدر

---

**Built with ❤️ for Arabic-speaking content creators**
