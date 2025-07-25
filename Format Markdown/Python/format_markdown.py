import markdown
from reportlab.lib.pagesizes import letter, A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_JUSTIFY
from reportlab.pdfgen import canvas
from reportlab.platypus import BaseDocTemplate, Frame, PageTemplate
from bs4 import BeautifulSoup
from bs4.element import Tag
import re
import os

def get_version() -> str:
    """Return the fixed version string for this code"""
    return "1.0.0"

def get_font_variant(base: str, variant: str) -> str:
    """Return the correct ReportLab font name for the given base and variant ('bold', 'italic', or 'bolditalic')."""
    if base == "Helvetica":
        if variant == "bold": return "Helvetica-Bold"
        if variant == "italic": return "Helvetica-Oblique"
        if variant == "bolditalic": return "Helvetica-BoldOblique"
        return "Helvetica"
    if base == "Times-Roman":
        if variant == "bold": return "Times-Bold"
        if variant == "italic": return "Times-Italic"
        if variant == "bolditalic": return "Times-BoldItalic"
        return "Times-Roman"
    if base == "Courier":
        if variant == "bold": return "Courier-Bold"
        if variant == "italic": return "Courier-Oblique"
        if variant == "bolditalic": return "Courier-BoldOblique"
        return "Courier"
    return base

class NumberedCanvas(canvas.Canvas):
    """Canvas with page numbering"""
    def __init__(self, *args, **kwargs):
        canvas.Canvas.__init__(self, *args, **kwargs)
        self._saved_page_states = []

    def showPage(self):
        self._saved_page_states.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        num_pages = len(self._saved_page_states)
        for state in self._saved_page_states:
            self.__dict__.update(state)
            self.draw_page_number(num_pages)
            canvas.Canvas.showPage(self)
        canvas.Canvas.save(self)

    def draw_page_number(self, total_pages):
        self.setFont("Helvetica", 9)
        self.setFillColor(colors.grey)
        self.drawRightString(
            letter[0] - 0.75 * inch, 
            0.5 * inch,
            f"Page {self._pageNumber} of {total_pages}"
        )

def markdown_to_pdf(
    input_md: str,
    output_pdf: str = None,
    primary_color: str = "#2c3e50",
    secondary_color: str = "#3498db",
    font_name: str = "Helvetica",
    margin: float = 0.75,
    table_header_color: str = None
) -> str:
    """
    Convert a Markdown file to a beautifully styled PDF using markdown and ReportLab.
    Allows customization of colors, font, and margins.
    Args:
        input_md: Path to input markdown file
        output_pdf: Path to output PDF file (optional)
        primary_color: Main color for headings and accents
        secondary_color: Secondary color for borders and highlights
        font_name: Base font name for text
        margin: Margin size in inches
        table_header_color: Color for table headers (optional, defaults to primary_color)
    Returns:
        Path to the generated PDF
    """
    if table_header_color is None:
        table_header_color = primary_color

    if output_pdf is None:
        output_pdf = os.path.splitext(input_md)[0] + ".pdf"

    with open(input_md, 'r', encoding='utf-8') as f:
        md_content = f.read()

    html_content = markdown.markdown(
        md_content,
        extensions=[
            'markdown.extensions.codehilite',
            'markdown.extensions.fenced_code',
            'markdown.extensions.tables',
            'markdown.extensions.toc',
            'markdown.extensions.attr_list',
            'markdown.extensions.def_list',
            'markdown.extensions.footnotes'
        ]
    )

    soup = BeautifulSoup(html_content, 'html.parser')

    doc = SimpleDocTemplate(
        output_pdf,
        pagesize=A4,
        rightMargin=margin*inch,
        leftMargin=margin*inch,
        topMargin=margin*inch,
        bottomMargin=margin*inch,
        canvasmaker=NumberedCanvas
    )

    styles = getSampleStyleSheet()

    title_style = ParagraphStyle(
        'CustomTitle',
        parent=styles['Heading1'],
        fontSize=24,
        spaceAfter=30,
        spaceBefore=0,
        textColor=colors.HexColor(primary_color),
        fontName=get_font_variant(font_name, "bold"),
        alignment=TA_LEFT,
        borderWidth=2,
        borderColor=colors.HexColor(secondary_color),
        borderPadding=8,
        backColor=colors.HexColor('#f8f9fa')
    )

    heading1_style = ParagraphStyle(
        'CustomHeading1',
        parent=styles['Heading1'],
        fontSize=18,
        spaceAfter=18,
        spaceBefore=24,
        textColor=colors.HexColor(primary_color),
        fontName=get_font_variant(font_name, "bold"),
        alignment=TA_LEFT,
        borderWidth=1,
        borderColor=colors.HexColor(secondary_color),
        borderPadding=4
    )

    heading2_style = ParagraphStyle(
        'CustomHeading2',
        parent=styles['Heading2'],
        fontSize=15,
        spaceAfter=15,
        spaceBefore=20,
        textColor=colors.HexColor(primary_color),
        fontName=get_font_variant(font_name, "bold"),
        alignment=TA_LEFT,
        leftIndent=8,
        borderWidth=0,
        borderColor=colors.HexColor(secondary_color),
        borderPadding=2
    )

    heading3_style = ParagraphStyle(
        'CustomHeading3',
        parent=styles['Heading3'],
        fontSize=13,
        spaceAfter=12,
        spaceBefore=16,
        textColor=colors.HexColor(primary_color),
        fontName=get_font_variant(font_name, "bold"),
        alignment=TA_LEFT,
        leftIndent=4
    )

    body_style = ParagraphStyle(
        'CustomBody',
        parent=styles['Normal'],
        fontSize=11,
        leading=16,
        spaceAfter=12,
        textColor=colors.black,  # Always black for normal text
        fontName=get_font_variant(font_name, None),
        alignment=TA_JUSTIFY,
        firstLineIndent=0
    )

    code_style = ParagraphStyle(
        'CustomCode',
        parent=styles['Code'],
        fontSize=9,
        leading=12,
        fontName="Courier",
        textColor=colors.HexColor(secondary_color),
        backColor=colors.HexColor('#f8f9fa'),
        borderColor=colors.HexColor('#e1e8ed'),
        borderWidth=1,
        borderPadding=8,
        leftIndent=20,
        rightIndent=20,
        spaceAfter=12
    )

    code_block_style = ParagraphStyle(
        'CustomCodeBlock',
        parent=styles['Code'],
        fontSize=9,
        leading=11,
        fontName="Courier",
        textColor=colors.HexColor(primary_color),
        backColor=colors.HexColor('#f8f9fa'),
        borderColor=colors.HexColor(secondary_color),
        borderWidth=1,
        borderPadding=12,
        spaceAfter=15,
        spaceBefore=5
    )

    quote_style = ParagraphStyle(
        'CustomQuote',
        parent=body_style,
        leftIndent=30,
        rightIndent=30,
        fontName=get_font_variant(font_name, "italic"),
        textColor=colors.HexColor('#555555'),
        backColor=colors.HexColor('#f8f9fa'),
        borderColor=colors.HexColor(secondary_color),
        borderWidth=0,
        borderPadding=15,
        spaceAfter=15,
        spaceBefore=5
    )

    bullet_style = ParagraphStyle(
        'CustomBullet',
        parent=body_style,
        leftIndent=20,
        bulletIndent=10,
        spaceAfter=6
    )

    story = []

    def clean_text(text):
        if not text:
            return ""
        text = text.replace('&lt;', '<').replace('&gt;', '>').replace('&amp;', '&')
        text = re.sub(r'\s+', ' ', text).strip()
        return text

    def process_element(element, level=0):
        if not element or not hasattr(element, 'name'):
            return
        tag = element.name
        text = clean_text(element.get_text())
        if tag == 'h1':
            if level == 0:
                story.append(Paragraph(text, title_style))
            else:
                story.append(Paragraph(text, heading1_style))
            story.append(Spacer(1, 12))
        elif tag == 'h2':
            story.append(Paragraph(text, heading2_style))
            story.append(Spacer(1, 8))
        elif tag == 'h3':
            story.append(Paragraph(text, heading3_style))
            story.append(Spacer(1, 6))
        elif tag in ['h4', 'h5', 'h6']:
            style = ParagraphStyle(
                f'Heading{tag[1]}',
                parent=body_style,
                fontSize=12 - int(tag[1]),
                fontName=get_font_variant(font_name, "bold"),
                spaceAfter=8,
                spaceBefore=12,
                textColor=colors.HexColor(primary_color)
            )
            story.append(Paragraph(text, style))
            story.append(Spacer(1, 4))
        elif tag == 'p':
            if text.strip():
                story.append(Paragraph(text, body_style))
                story.append(Spacer(1, 6))
        elif tag == 'pre':
            code_text = element.get_text()
            lines = code_text.split('\n')
            for line in lines:
                if line.strip():
                    escaped_line = line.replace('<', '&lt;').replace('>', '&gt;').replace('&', '&amp;')
                    story.append(Paragraph(escaped_line, code_block_style))
            story.append(Spacer(1, 10))
        elif tag == 'code' and element.parent.name != 'pre':
            if text.strip():
                escaped_text = text.replace('<', '&lt;').replace('>', '&gt;').replace('&', '&amp;')
                story.append(Paragraph(f"<font name='Courier' color='{secondary_color}'>{escaped_text}</font>", body_style))
        elif tag == 'ul':
            for li in element.find_all('li', recursive=False):
                li_text = clean_text(li.get_text())
                if li_text:
                    story.append(Paragraph(f"• {li_text}", bullet_style))
            story.append(Spacer(1, 8))
        elif tag == 'ol':
            for i, li in enumerate(element.find_all('li', recursive=False), 1):
                li_text = clean_text(li.get_text())
                if li_text:
                    story.append(Paragraph(f"{i}. {li_text}", bullet_style))
            story.append(Spacer(1, 8))
        elif tag == 'table':
            table_data = []
            header_row = element.find('tr')
            if header_row:
                headers = []
                for th in header_row.find_all(['th', 'td']):
                    headers.append(clean_text(th.get_text()))
                if headers:
                    table_data.append(headers)
            for row in element.find_all('tr')[1:]:
                row_data = []
                for cell in row.find_all(['td', 'th']):
                    row_data.append(clean_text(cell.get_text()))
                if row_data:
                    table_data.append(row_data)
            if table_data:
                table = Table(table_data, hAlign='LEFT', splitByRow=0)
                table.setStyle(TableStyle([
                    ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor(table_header_color)),
                    ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
                    ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
                    ('FONTNAME', (0, 0), (-1, 0), get_font_variant(font_name, "bold")),
                    ('FONTSIZE', (0, 0), (-1, 0), 10),
                    ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
                    ('BACKGROUND', (0, 1), (-1, -1), colors.HexColor('#f8f9fa')),
                    ('FONTNAME', (0, 1), (-1, -1), get_font_variant(font_name, None)),
                    ('FONTSIZE', (0, 1), (-1, -1), 9),
                    ('GRID', (0, 0), (-1, -1), 1, colors.HexColor(secondary_color)),
                    ('VALIGN', (0, 0), (-1, -1), 'TOP'),
                    ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, colors.HexColor('#f8f9fa')])
                ]))
                story.append(table)
                story.append(Spacer(1, 15))
        elif tag == 'blockquote':
            quote_text = clean_text(element.get_text())
            if quote_text:
                story.append(Paragraph(f'"{quote_text}"', quote_style))
                story.append(Spacer(1, 12))
        elif tag == 'hr':
            story.append(Spacer(1, 12))
            line_style = ParagraphStyle(
                'HorizontalLine',
                parent=body_style,
                borderWidth=1,
                borderColor=colors.HexColor(secondary_color),
                spaceAfter=12
            )
            story.append(Paragraph("", line_style))
        if isinstance(element, Tag):
            for child in element.children:
                if hasattr(child, 'name'):
                    process_element(child, level + 1)
    h1_count = 0
    for element in soup.children:
        if hasattr(element, 'name'):
            if element.name == 'h1':
                h1_count += 1
            process_element(element, h1_count)
    try:
        doc.build(story)
        print(f"✅ Successfully generated PDF: {output_pdf}")
    except Exception as e:
        print(f"❌ Error generating PDF: {e}")
        raise
    return output_pdf

# Example usage
if __name__ == "__main__":
    # Test with a sample markdown file
    test_md = """# Sample Document

This is a **beautiful** markdown document with various elements.

## Features

- Beautiful typography
- Professional styling  
- Code highlighting
- Tables and more

### Table Example

| Feature | Status |
|---------|--------|
| PDF Generation | ✅ Complete |
| Styling | ✅ Beautiful |
| Cross-platform | ✅ Works everywhere |

> This is a blockquote that looks elegant in the PDF.

That's it! Pure Python, beautiful results.
"""
    
    # Create test file
    with open("test.md", "w", encoding="utf-8") as f:
        f.write(test_md)
    
    # Generate PDF
    pdf_path = markdown_to_pdf("test.md", "beautiful_output.pdf")
    print(f"PDF generated: {pdf_path}")