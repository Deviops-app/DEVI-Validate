using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Devi.Brand;

using System.Security.Cryptography.X509Certificates;
using DeviValidate.Core.Packaging;
using PdfSharp.Pdf.Signatures;

namespace DeviValidate.Core.Reporting;

public static class PdfReport
{
	private enum Tone
	{
		Match,
		Mismatch,
		Missing,
		Review
	}

	private sealed class PdfWriter
	{
		private const double PageWidth = 612.0;

		private const double PageHeight = 792.0;

		private const double Margin = 46.0;

		private const double FooterHeight = 36.0;

		private readonly string _runningTitle;

		private readonly string _footerLabel;

		private readonly string _recordHash;

		private readonly PdfDocument _document = new PdfDocument();

		public X509Certificate2? Signer { get; set; }

		public string? SignatureReason { get; set; }

		private XGraphics? _graphics;

		private int _pageNumber;

		private double _y;

		private readonly Dictionary<XFont, double> _lineHeights = new Dictionary<XFont, double>();

		private MemoryStream? _wordmarkStream;

		private MemoryStream? _markStream;

		private XImage? _wordmark;

		private XImage? _mark;

		public XFont TitleFont { get; private set; }

		public XFont HeadingFont { get; private set; }

		public XFont Body { get; private set; }

		public XFont BodyBold { get; private set; }

		public XFont Small { get; private set; }

		public XFont Kicker { get; private set; }

		public XFont Number { get; private set; }

		public XFont Mono { get; private set; }

		public XBrush Ink { get; private set; }

		public XBrush Muted { get; private set; }

		public XBrush White { get; private set; }

		public XBrush DangerText { get; private set; }

		public XBrush SuccessText { get; private set; }

		private XBrush Soft { get; set; }

		private XBrush DangerSoft { get; set; }

		private XPen Rule { get; set; }

		private XColor Accent { get; set; }

		private XColor Danger { get; set; }

		private XColor Success { get; set; }

		private XColor NearBlack { get; set; }

		private XGraphics Gfx => _graphics ?? throw new InvalidOperationException("The PDF page is not open.");

		private double ContentWidth => 520.0;

		private double ContentBottom => 748.0;

		public PdfWriter(string runningTitle, string footerLabel, string recordHash)
		{
			_runningTitle = runningTitle;
			_footerLabel = footerLabel;
			_recordHash = recordHash;
		}

		public byte[] Render(Action<PdfWriter> draw)
		{
			PdfFonts.Ensure();
			_document.Info.Title = "DEVI Validate " + _runningTitle;
			_document.Info.Author = "The DEVI Validate authors";
			_document.Info.Creator = "DEVI Validate 1.0.3";
			_document.Info.Subject = _runningTitle;
			_document.Options.CompressContentStreams = false;
			TitleFont = new XFont("Devi Sans", 18.0, XFontStyleEx.Bold);
			HeadingFont = new XFont("Devi Sans", 12.0, XFontStyleEx.Bold);
			Body = new XFont("Devi Sans", 10.0, XFontStyleEx.Regular);
			BodyBold = new XFont("Devi Sans", 10.0, XFontStyleEx.Bold);
			Small = new XFont("Devi Sans", 8.5, XFontStyleEx.Regular);
			Kicker = new XFont("Devi Sans", 8.0, XFontStyleEx.Bold);
			Number = new XFont("Devi Sans", 16.0, XFontStyleEx.Bold);
			Mono = new XFont("Devi Mono", 8.0, XFontStyleEx.Regular);
			NearBlack = DeviPdfPalette.Ink;
			Accent = DeviPdfPalette.Accent;
			Danger = DeviPdfPalette.Danger;
			Success = DeviPdfPalette.Success;
			Ink = new XSolidBrush(NearBlack);
			Muted = new XSolidBrush(DeviPdfPalette.Muted);
			White = new XSolidBrush(DeviPdfPalette.Paper);
			DangerText = new XSolidBrush(Danger);
			SuccessText = new XSolidBrush(Success);
			Soft = new XSolidBrush(DeviPdfPalette.Soft);
			DangerSoft = new XSolidBrush(DeviPdfPalette.DangerSoft);
			Rule = new XPen(DeviPdfPalette.RuleLine, 0.7);
			_wordmarkStream = new MemoryStream(BrandAssets.WordmarkWhitePng);
			_markStream = new MemoryStream(BrandAssets.WordmarkCompactWhitePng);
			_wordmark = XImage.FromStream(_wordmarkStream);
			_mark = XImage.FromStream(_markStream);
			FreshPage();
			draw(this);
			_graphics?.Dispose();
			if (Signer != null)
			{
				DigitalSignatureOptions options = new DigitalSignatureOptions
				{
					AppName = "DEVI Validate " + ToolInfo.Version,
					Reason = SignatureReason ?? "Validation package",
					Location = Environment.MachineName,
					ContactInfo = Signer.GetNameInfo(X509NameType.EmailName, false) ?? ""
				};
				DigitalSignatureHandler.ForDocument(_document, new PdfSharpDefaultSigner(Signer, PdfMessageDigestType.SHA256), options);
			}
			using MemoryStream memoryStream = new MemoryStream();
			_document.Save(memoryStream);
			_wordmark.Dispose();
			_mark.Dispose();
			_wordmarkStream.Dispose();
			_markStream.Dispose();
			_document.Dispose();
			return memoryStream.ToArray();
		}

		public void FreshPage()
		{
			_graphics?.Dispose();
			PdfPage pdfPage = _document.AddPage();
			pdfPage.Size = PageSize.Letter;
			_graphics = XGraphics.FromPdfPage(pdfPage);
			_pageNumber++;
			double band = DeviPdfBand.Draw(_graphics, 612.0, 46.0, (_pageNumber > 1) ? _mark : _wordmark, _pageNumber > 1, "Validate", (_pageNumber > 1) ? _runningTitle : ToolInfo.Version, "Devi Sans");
			double num = 756.0;
			_graphics.DrawLine(Rule, 46.0, num, 566.0, num);
			_graphics.DrawString(_footerLabel + "  " + _recordHash, Mono, Ink, new XRect(46.0, num + 8.0, 484.0, 14.0), XStringFormats.TopLeft);
			string text = _pageNumber.ToString(CultureInfo.InvariantCulture);
			_graphics.DrawString(text, Small, Muted, new XRect(542.0, num + 8.0, 24.0, 14.0), XStringFormats.TopLeft);
			_y = band + 16.0;
		}

		public void Title(string text)
		{
			if (_pageNumber == 1)
			{
				Gfx.DrawString(text.ToUpperInvariant(), Kicker, Muted, new XRect(46.0, _y, ContentWidth, 12.0), XStringFormats.TopLeft);
				_y += 16.0;
			}
			else
			{
				Ensure(20.0);
				Gfx.DrawString(text, HeadingFont, Ink, new XRect(46.0, _y, ContentWidth, 16.0), XStringFormats.TopLeft);
				_y += 22.0;
			}
		}

		public void Verdict(Tone tone, string text, string? kicker = null)
		{
			List<string> list = Wrap(text, BodyBold, ContentWidth - 28.0);
			double num = 22.0 + (double)list.Count * LineHeight(BodyBold) + 12.0;
			Ensure(num + 8.0);
			XRect rect = new XRect(46.0, _y, ContentWidth, num);
			XBrush brush = tone switch
			{
				Tone.Match => new XSolidBrush(DeviPdfPalette.SuccessSoft), 
				Tone.Mismatch => new XSolidBrush(Danger), 
				_ => new XSolidBrush(DeviPdfPalette.Paper), 
			};
			XBrush brush2 = tone switch
			{
				Tone.Match => SuccessText, 
				Tone.Mismatch => White, 
				_ => Ink, 
			};
			Gfx.DrawRectangle(brush, rect);
			if ((uint)(tone - 2) <= 1u)
			{
				Gfx.DrawRectangle(new XPen(Accent, 1.4), rect);
			}
			if (kicker == null)
			{
				kicker = tone switch
				{
					Tone.Match => "MATCH", 
					Tone.Mismatch => "DOES NOT MATCH", 
					Tone.Missing => "FILE NOT FOUND", 
					_ => "REVIEW THE FILE LIST", 
				};
			}
			Gfx.DrawString(kicker, Kicker, brush2, new XRect(60.0, _y + 10.0, ContentWidth - 28.0, 12.0), XStringFormats.TopLeft);
			double num2 = _y + 24.0;
			foreach (string item in list)
			{
				Gfx.DrawString(item, BodyBold, brush2, new XRect(60.0, num2, ContentWidth - 28.0, LineHeight(BodyBold)), XStringFormats.TopLeft);
				num2 += LineHeight(BodyBold);
			}
			_y += num + 10.0;
		}

		public void Fields(IReadOnlyList<RecordFields.Field> fields)
		{
			foreach (RecordFields.Field field in fields)
			{
				XFont font = (field.Mono ? Mono : Body);
				List<string> list = Wrap(field.Value, font, ContentWidth - 168.0);
				double num = Math.Max(LineHeight(Small), (double)list.Count * LineHeight(font));
				Ensure(num + 3.0);
				Gfx.DrawString(field.Label, Small, Muted, new XRect(46.0, _y + 1.0, 158.0, num), XStringFormats.TopLeft);
				double num2 = _y;
				foreach (string item in list)
				{
					Gfx.DrawString(item, font, Ink, new XRect(214.0, num2, ContentWidth - 168.0, LineHeight(font)), XStringFormats.TopLeft);
					num2 += LineHeight(font);
				}
				_y += num + 3.0;
			}
		}

		public void Stats(IReadOnlyList<(string Label, int Count)> stats)
		{
			double num = 8.0;
			double num2 = (ContentWidth - num * (double)(stats.Count - 1)) / (double)stats.Count;
			Ensure(46.0);
			for (int i = 0; i < stats.Count; i++)
			{
				bool flag = stats[i].Count > 0;
				if (flag)
				{
					string item = stats[i].Label;
					bool flag2 = ((item == "Mismatch" || item == "Failed") ? true : false);
					flag = flag2;
				}
				bool flag3 = flag;
				double num3 = 46.0 + (double)i * (num2 + num);
				Gfx.DrawRectangle(flag3 ? DangerSoft : Soft, new XRect(num3, _y, num2, 46.0));
				Gfx.DrawString(stats[i].Count.ToString(CultureInfo.InvariantCulture), Number, flag3 ? DangerText : Ink, new XRect(num3 + 8.0, _y + 4.0, num2 - 16.0, 22.0), XStringFormats.TopLeft);
				Gfx.DrawString(stats[i].Label, Small, flag3 ? DangerText : Muted, new XRect(num3 + 8.0, _y + 26.0, num2 - 16.0, 12.0), XStringFormats.TopLeft);
			}
			_y += 46.0;
		}

		public void Table(string[] headers, double[] widths, IReadOnlyList<string[]> rows, int[] monoColumns, int pathColumn = -1, int sizeColumn = -1)
		{
			DrawHeader(headers, widths, sizeColumn);
			foreach (string[] row in rows)
			{
				double num = RowHeight(row, widths, monoColumns, pathColumn, sizeColumn);
				if (_y + num > ContentBottom)
				{
					FreshPage();
					DrawHeader(headers, widths, sizeColumn);
				}
				bool flag = row.Any((string cell) => (cell == "Mismatch" || cell == "Fail") ? true : false);
				if (flag)
				{
					Gfx.DrawRectangle(DangerSoft, new XRect(46.0, _y, ContentWidth, num));
				}
				double num2 = 46.0;
				for (int i = 0; i < headers.Length; i++)
				{
					XFont xFont = (monoColumns.Contains(i) ? Mono : Body);
					List<string> list = CellLines(row[i], xFont, widths[i] - 8.0, i, pathColumn, sizeColumn);
					bool flag2 = i == sizeColumn && list.Count > 1;
					double num3 = _y + 3.0;
					XBrush brush = (flag ? DangerText : Ink);
					for (int j = 0; j < list.Count; j++)
					{
						XFont font = ((flag2 && j > 0) ? Small : xFont);
						bool flag3 = i == sizeColumn;
						XRect layoutRectangle = (flag3 ? new XRect(num2 + 4.0, num3, widths[i] - 8.0, LineHeight(font)) : new XRect(num2, num3, widths[i] - 8.0, LineHeight(font)));
						Gfx.DrawString(list[j], font, brush, layoutRectangle, flag3 ? XStringFormats.TopRight : XStringFormats.TopLeft);
						num3 += LineHeight(font);
					}
					num2 += widths[i];
				}
				_y += num;
				Gfx.DrawLine(flag ? new XPen(Danger, 0.8) : Rule, 46.0, _y, 46.0 + ContentWidth, _y);
			}
		}

		public void Qr(bool[][] modules, double x, double y, double size)
		{
			int n = modules.Length;
			double cell = size / n;
			XBrush dark = Ink;
			for (int r = 0; r < n; r++)
			{
				for (int c = 0; c < n; c++)
				{
					if (modules[r][c])
					{
						Gfx.DrawRectangle(dark, x + c * cell, y + r * cell, cell + 0.05, cell + 0.05);
					}
				}
			}
		}

		/// <summary>Boxed verification code with its QR code, used on page one and the package page.</summary>
		public void PackageBox(PackageStamp stamp)
		{
			const double qr = 74.0;
			const double pad = 10.0;
			double height = qr + pad * 2;
			Ensure(height + 8.0);
			double top = _y;
			Gfx.DrawRectangle(Rule, Soft, 46.0, top, ContentWidth, height);
			Qr(QrMatrix.Create(stamp.QrPayload), 46.0 + pad, top + pad, qr);
			double tx = 46.0 + pad * 2 + qr + 4.0;
			double tw = ContentWidth - (tx - 46.0) - pad;
			Gfx.DrawString("VALIDATION PACKAGE", Kicker, Muted, new XRect(tx, top + pad, tw, 12.0), XStringFormats.TopLeft);
			Gfx.DrawString(stamp.VerificationCode, Number, Ink, new XRect(tx, top + pad + 13.0, tw, 20.0), XStringFormats.TopLeft);
			double ly = top + pad + 36.0;
			foreach (string line in Wrap("Verification code: the first 20 characters of the record SHA-256 in the footer. Scan the QR code or type this code into DEVI Validate (Verify a package) to re-check the package offline." + (stamp.SignerSubject == null ? " This package is hash-sealed and not signed." : " Signed by " + stamp.SignerSubject + "."), Small, tw))
			{
				Gfx.DrawString(line, Small, Muted, new XRect(tx, ly, tw, LineHeight(Small)), XStringFormats.TopLeft);
				ly += LineHeight(Small);
			}
			_y = top + height + 8.0;
		}

		public void Heading(string text)
		{
			Gap(8.0);
			Ensure(16.0);
			Gfx.DrawString(text, HeadingFont, Ink, new XRect(46.0, _y, ContentWidth, 16.0), XStringFormats.TopLeft);
			_y += 18.0;
		}

		public void Paragraph(string text, XFont font, XBrush brush)
		{
			foreach (string item in Wrap(text, font, ContentWidth))
			{
				double num = LineHeight(font);
				Ensure(num);
				Gfx.DrawString(item, font, brush, new XRect(46.0, _y, ContentWidth, num), XStringFormats.TopLeft);
				_y += num;
			}
		}

		public void Gap(double points)
		{
			_y += points;
		}

		private void DrawHeader(string[] headers, double[] widths, int sizeColumn)
		{
			Ensure(18.0);
			double num = 46.0;
			for (int i = 0; i < headers.Length; i++)
			{
				bool flag = i == sizeColumn;
				XRect layoutRectangle = (flag ? new XRect(num + 4.0, _y, widths[i] - 8.0, 12.0) : new XRect(num, _y, widths[i] - 6.0, 12.0));
				Gfx.DrawString(headers[i], Kicker, Muted, layoutRectangle, flag ? XStringFormats.TopRight : XStringFormats.TopLeft);
				num += widths[i];
			}
			_y += 14.0;
			Gfx.DrawLine(new XPen(NearBlack, 0.8), 46.0, _y, 46.0 + ContentWidth, _y);
			_y += 2.0;
		}

		private double RowHeight(string[] row, double[] widths, int[] monoColumns, int pathColumn, int sizeColumn)
		{
			double num = 16.0;
			for (int i = 0; i < row.Length; i++)
			{
				XFont font = (monoColumns.Contains(i) ? Mono : Body);
				List<string> list = CellLines(row[i], font, widths[i] - 8.0, i, pathColumn, sizeColumn);
				double num2 = ((i != sizeColumn || list.Count <= 1) ? ((double)list.Count * LineHeight(font)) : (LineHeight(font) + (double)(list.Count - 1) * LineHeight(Small)));
				num = Math.Max(num, num2 + 6.0);
			}
			return num;
		}

		private List<string> CellLines(string text, XFont font, double width, int column, int pathColumn, int sizeColumn)
		{
			XFont font2 = font;
			Func<string, double> measure = (string value) => Measure(value, font2);
			if (column == pathColumn)
			{
				return TableWrap.WrapPath(text, width, measure);
			}
			if (column == sizeColumn)
			{
				return TableWrap.SizeLines(text, width, measure);
			}
			return Wrap(text, font2, width);
		}

		private List<string> Wrap(string text, XFont font, double width)
		{
			List<string> list = new List<string>();
			string[] array = (string.IsNullOrEmpty(text) ? "-" : text).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
			foreach (string text2 in array)
			{
				if (text2.Length == 0)
				{
					list.Add("");
					continue;
				}
				string text3 = "";
				string[] array2 = text2.Split(' ');
				foreach (string text4 in array2)
				{
					string text5 = ((text3.Length == 0) ? text4 : (text3 + " " + text4));
					if (Measure(text5, font) <= width)
					{
						text3 = text5;
						continue;
					}
					if (text3.Length > 0)
					{
						list.Add(text3);
					}
					text3 = ((!(Measure(text4, font) <= width)) ? BreakLong(text4, font, width, list) : text4);
				}
				if (text3.Length > 0)
				{
					list.Add(text3);
				}
			}
			if (list.Count == 0)
			{
				list.Add("");
			}
			return list;
		}

		private string BreakLong(string word, XFont font, double width, List<string> lines)
		{
			string text = "";
			for (int i = 0; i < word.Length; i++)
			{
				char c = word[i];
				string text2 = text + c;
				if (Measure(text2, font) <= width)
				{
					text = text2;
					continue;
				}
				if (text.Length > 0)
				{
					lines.Add(text);
				}
				text = c.ToString();
			}
			return text;
		}

		private double Measure(string text, XFont font)
		{
			return Gfx.MeasureString((text.Length == 0) ? " " : text, font).Width;
		}

		private double LineHeight(XFont font)
		{
			if (_lineHeights.TryGetValue(font, out var value))
			{
				return value;
			}
			value = Gfx.MeasureString("Mg", font).Height + 1.0;
			_lineHeights[font] = value;
			return value;
		}

		private void Ensure(double height)
		{
			if (!(_y + height <= ContentBottom))
			{
				FreshPage();
			}
		}
	}

	public static byte[] Render(VerificationRecord record)
	{
		return Render(record, null, null);
	}

	/// <summary>The verification record PDF. With a stamp it adds the verification code, QR code, and package page; with a signer it is signed.</summary>
	public static byte[] Render(VerificationRecord record, PackageStamp? stamp, X509Certificate2? signer)
	{
		VerificationRecord record2 = record;
		PdfWriter writer = new PdfWriter(stamp == null ? "Verification record" : "Validation package", "Record SHA-256", record2.Integrity.Hash)
		{
			Signer = signer,
			SignatureReason = "DEVI Validate validation package " + stamp?.VerificationCode
		};
		return writer.Render(delegate(PdfWriter page)
		{
			page.Title(stamp == null ? "Verification record" : "Validation package");
			Tone tone = VerdictTone(record2);
			page.Verdict(tone, record2.Verdict);
			if (stamp != null)
			{
				page.PackageBox(stamp);
			}
			if (record2.MismatchCount > 0)
			{
				page.Paragraph("Red means the new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. The notes below say what that can and cannot mean.", page.Body, page.DangerText);
				page.Gap(6.0);
			}
			else if (tone == Tone.Match)
			{
				page.Paragraph("A match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Anyone can check the file again with sha256sum, certutil -hashfile, or Get-FileHash. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete.", page.Body, page.SuccessText);
				page.Gap(6.0);
			}
			page.Paragraph("This record independently recomputes hash values and compares them with values recorded by another tool or process.", page.Body, page.Ink);
			page.Gap(8.0);
			page.Fields(RecordFields.Verification(record2));
			page.Paragraph(RecordFields.FileNameNote(record2), page.Small, page.Muted);
			if (!string.IsNullOrWhiteSpace(record2.Note))
			{
				page.Gap(6.0);
				page.Paragraph(record2.Note, page.Small, page.Muted);
			}
			page.Gap(12.0);
			page.Stats(System.Array.AsReadOnly(new(string, int)[4]
			{
				("Match", record2.MatchCount),
				("Mismatch", record2.MismatchCount),
				("Missing", record2.MissingCount),
				("Extra", record2.ExtraCount)
			}));
			page.Gap(12.0);
			page.Table(new string[5] { "Result", "Path", "Size", "Computed hash", "Expected hash" }, new double[5] { 62.0, 166.0, 96.0, 98.0, 98.0 }, record2.Files.Select(delegate(FileVerificationEntry file)
			{
				string[] obj = new string[5]
				{
					ResultLabels.Display(file.Result),
					file.Path,
					null,
					null,
					null
				};
				long? size = file.Size;
				object obj2;
				if (size.HasValue)
				{
					long valueOrDefault = size.GetValueOrDefault();
					obj2 = ByteSize.Format(valueOrDefault);
				}
				else
				{
					obj2 = "-";
				}
				obj[2] = (string)obj2;
				obj[3] = file.ComputedHash ?? "-";
				obj[4] = file.ExpectedHash ?? "-";
				return obj;
			}).ToList(), new int[2] { 3, 4 }, 1, 2);
			if (record2.Skipped.Count > 0)
			{
				page.Gap(8.0);
				page.Paragraph(ReportCopy.SkippedSummary(record2.Skipped.Count), page.Small, page.Muted);
				foreach (SkippedEntry item in record2.Skipped)
				{
					page.Paragraph(item.Path + ": " + ReportCopy.SkippedDetail(item), page.Small, page.Ink);
				}
			}
			page.FreshPage();
			page.Title("How to read this record");
			foreach (ReportSection item2 in ReportCopy.Explanation)
			{
				page.Heading(item2.Heading);
				string[] array = item2.Body.Split("\n\n");
				foreach (string text in array)
				{
					page.Paragraph(text, page.Body, page.Ink);
					page.Gap(4.0);
				}
				if (item2.Heading == "Validation and use")
				{
					page.Fields(System.Array.AsReadOnly(new RecordFields.Field[3]
					{
						new RecordFields.Field("Validated by", ReportCopy.OrNotRecorded(record2.ValidatedBy), Mono: false),
						new RecordFields.Field("Validation date", ReportCopy.OrNotRecorded(record2.ValidationDate), Mono: false),
						new RecordFields.Field("Lab procedure/reference", ReportCopy.OrNotRecorded(record2.LabProcedure), Mono: false)
					}));
				}
			}
			if (stamp != null)
			{
				page.FreshPage();
				page.Title("Package and re-verification");
				page.PackageBox(stamp);
				page.Fields(System.Array.AsReadOnly(new RecordFields.Field[]
				{
					new RecordFields.Field("Verification code", stamp.VerificationCode, Mono: true),
					new RecordFields.Field("Record SHA-256", stamp.RecordSha256, Mono: true),
					new RecordFields.Field("QR code text", stamp.QrPayload, Mono: true),
					new RecordFields.Field("Verified (UTC)", ValidationPackage.Utc(record2.VerifiedAt), Mono: false),
					new RecordFields.Field("Verified (local)", ValidationPackage.LocalWithZone(record2.VerifiedAt, record2.TimeZone), Mono: false),
					new RecordFields.Field("Package written (UTC)", ValidationPackage.Utc(stamp.CreatedAt), Mono: false),
					new RecordFields.Field("Package written (local)", ValidationPackage.LocalWithZone(stamp.CreatedAt, stamp.TimeZone), Mono: false),
					new RecordFields.Field("Examiner", ReportCopy.OrNotRecorded(record2.Examiner), Mono: false),
					new RecordFields.Field("Agency/department", ReportCopy.OrNotRecorded(record2.Agency), Mono: false),
					new RecordFields.Field("Case or reference", ReportCopy.OrNotRecorded(record2.CaseReference), Mono: false),
					new RecordFields.Field("Validated by", ReportCopy.OrNotRecorded(record2.ValidatedBy), Mono: false),
					new RecordFields.Field("Validation date", ReportCopy.OrNotRecorded(record2.ValidationDate), Mono: false),
					new RecordFields.Field("Lab procedure/reference", ReportCopy.OrNotRecorded(record2.LabProcedure), Mono: false),
					new RecordFields.Field("Tool", ToolInfo.Name + " " + ToolInfo.Version, Mono: false),
					new RecordFields.Field("Executable", ReportCopy.OrNotRecorded(stamp.Executable), Mono: false),
					new RecordFields.Field("Executable SHA-256", ReportCopy.OrNotRecorded(stamp.ExecutableSha256), Mono: true),
					new RecordFields.Field("Core library SHA-256", stamp.CoreSha256 ?? "Inside the executable (single-file build)", Mono: stamp.CoreSha256 != null),
					new RecordFields.Field("Signature", stamp.SignerSubject == null ? "Not signed. Hash-sealed only." : stamp.SignerSubject, Mono: false),
					new RecordFields.Field("Signer thumbprint", ReportCopy.OrNotRecorded(stamp.SignerThumbprint), Mono: true)
				}));
				page.Gap(10.0);
				page.Heading("How to re-verify this package");
				page.Paragraph(ValidationPackage.HowToVerifyText, page.Body, page.Ink);
				page.Gap(6.0);
				page.Paragraph("This PDF is one file in the package. Its own SHA-256 is in package-manifest.json, which is written after the PDF, so the PDF cannot print it. The verification code ties this printed page to verification-record.json.", page.Small, page.Muted);
			}
		});
	}

	public static byte[] Render(SelfTestRecord record)
	{
		SelfTestRecord record2 = record;
		Tone tone = ((record2.FailedCount != 0) ? Tone.Mismatch : Tone.Match);
		return new PdfWriter("Tool validation record", "Record SHA-256", record2.Integrity.Hash).Render(delegate(PdfWriter page)
		{
			page.Title("Tool validation record");
			page.Verdict(tone, record2.Result, (record2.FailedCount == 0) ? "PASS" : "FAIL");
			page.Paragraph("This is a tool validation record from the built-in self-test. It is not an evidence verification. The checks hash published test vectors for SHA-256, SHA-1, and MD5, and they confirm that a synthetic file's contents and timestamps were unchanged after hashing. A passing result means this build reproduced the published values and the read-only check on this machine. It does not certify the tool for casework.", page.Body, page.Ink);
			page.Gap(8.0);
			page.Fields(RecordFields.SelfTest(record2));
			page.Gap(8.0);
			page.Stats(System.Array.AsReadOnly(new(string, int)[2]
			{
				("Passed", record2.PassedCount),
				("Failed", record2.FailedCount)
			}));
			page.Gap(12.0);
			page.Table(new string[5] { "Check", "Input", "Expected", "Computed", "Result" }, new double[5] { 78.0, 108.0, 132.0, 132.0, 70.0 }, record2.Checks.Select((SelfTestCheck check) => new string[5]
			{
				check.Name,
				check.Input,
				check.Expected,
				check.Computed,
				check.Passed ? "Pass" : "Fail"
			}).ToList(), new int[2] { 2, 3 });
		});
	}

	private static Tone VerdictTone(VerificationRecord record)
	{
		if (record.MismatchCount > 0)
		{
			return Tone.Mismatch;
		}
		if (record.MissingCount > 0)
		{
			return Tone.Missing;
		}
		if (record.ExtraCount > 0)
		{
			return Tone.Review;
		}
		return Tone.Match;
	}
}
