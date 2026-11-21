// Builds one Word file per role from the walkthroughs (docs/guides/walkthroughs/*.md), with the pictures and a run summary on top.
//   node demo/build-docx.mjs        (run take-screenshots.mjs first; the summary reads demo/output/results.json)
// Output: docs/guides/walkthroughs/docx/BannerAI-Demo-<Role>.docx
import {
  AlignmentType, BorderStyle, Document, HeadingLevel, ImageRun, Packer, PageBreak, Paragraph, ShadingType, Table, TableCell, TableRow, TextRun, WidthType,
} from 'docx'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const here = path.dirname(fileURLToPath(import.meta.url))
const WALK = path.join(here, '..', 'docs', 'guides', 'walkthroughs')
const OUT = path.join(WALK, 'docx')
fs.mkdirSync(OUT, { recursive: true })

const ROLES = [
  { file: 'owner.md', title: 'Shop owner', out: 'BannerAI-Demo-Shop-Owner.docx' },
  { file: 'executive.md', title: 'Sales executive', out: 'BannerAI-Demo-Sales-Executive.docx' },
  { file: 'admin.md', title: 'Administrator', out: 'BannerAI-Demo-Administrator.docx' },
]

const run = fs.existsSync(path.join(here, 'output', 'results.json'))
  ? JSON.parse(fs.readFileSync(path.join(here, 'output', 'results.json'), 'utf8'))
  : null
const byName = new Map((run?.results ?? []).map((r) => [r.name, r]))

// ----- inline text: **bold**, *italic*, `code`, [link](url)
function runs(text, base = {}) {
  const out = []
  const pattern = /(\*\*[^*]+\*\*|\*[^*]+\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g
  let last = 0
  for (const m of text.matchAll(pattern)) {
    if (m.index > last) out.push(new TextRun({ text: text.slice(last, m.index), ...base }))
    const t = m[0]
    if (t.startsWith('**')) out.push(new TextRun({ text: t.slice(2, -2), bold: true, ...base }))
    else if (t.startsWith('`')) out.push(new TextRun({ text: t.slice(1, -1), font: 'Consolas', shading: { type: ShadingType.CLEAR, fill: 'EEEEEE' }, ...base }))
    else if (t.startsWith('[')) out.push(new TextRun({ text: t.slice(1, t.indexOf(']')), color: '1D4ED8', ...base }))
    else out.push(new TextRun({ text: t.slice(1, -1), italics: true, ...base }))
    last = m.index + t.length
  }
  if (last < text.length) out.push(new TextRun({ text: text.slice(last), ...base }))
  return out
}

function pngSize(file) {
  const b = fs.readFileSync(file)
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20) }
}

function picture(rel, alt) {
  const file = path.join(WALK, rel)
  if (!fs.existsSync(file)) {
    return new Paragraph({ children: [new TextRun({ text: `[Picture missing: ${rel}]`, color: 'B91C1C', bold: true })] })
  }
  const { w, h } = pngSize(file)
  const width = 600
  return new Paragraph({
    spacing: { before: 120, after: 160 },
    alignment: AlignmentType.CENTER,
    children: [new ImageRun({ type: 'png', data: fs.readFileSync(file), transformation: { width, height: Math.round((width * h) / w) }, altText: { title: alt, description: alt, name: alt } })],
  })
}

const cell = (text, opts = {}) => new TableCell({
  width: opts.width ? { size: opts.width, type: WidthType.PERCENTAGE } : undefined,
  shading: opts.fill ? { type: ShadingType.CLEAR, fill: opts.fill } : undefined,
  margins: { top: 60, bottom: 60, left: 100, right: 100 },
  children: [new Paragraph({ children: runs(text, { bold: opts.bold, color: opts.color, size: 20 }) })],
})

const table = (rows, widths) => new Table({
  width: { size: 100, type: WidthType.PERCENTAGE },
  rows: rows.map((r, i) => new TableRow({ tableHeader: i === 0, children: r.map((c, j) => cell(c, { bold: i === 0, fill: i === 0 ? 'E5E7EB' : undefined, width: widths?.[j] })) })),
})

// ----- markdown -> blocks, and the steps (with their pictures) for the summary
function convert(md) {
  const lines = md.replace(/\r/g, '').split('\n')
  const blocks = []
  const steps = []
  let current = null
  let para = []
  const flush = () => {
    if (para.length) blocks.push(new Paragraph({ spacing: { after: 120 }, children: runs(para.join(' ')) }))
    para = []
  }
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]
    let m
    if (!line.trim()) { flush(); continue }
    if ((m = line.match(/^# (.*)/))) { flush(); blocks.push(new Paragraph({ heading: HeadingLevel.HEADING_1, children: runs(m[1]) })); continue }
    if ((m = line.match(/^## (.*)/))) {
      flush()
      current = { title: m[1], images: [] }
      steps.push(current)
      blocks.push(new Paragraph({ heading: HeadingLevel.HEADING_2, spacing: { before: 300 }, children: runs(m[1]) }))
      continue
    }
    if (/^---+$/.test(line.trim())) { flush(); continue }
    if ((m = line.match(/^!\[([^\]]*)\]\(([^)]+)\)/))) {
      flush()
      const rel = m[2]
      current?.images.push(path.basename(rel, '.png'))
      blocks.push(picture(rel, m[1]))
      continue
    }
    if ((m = line.match(/^(\d+)\. (.*)/))) {
      flush()
      blocks.push(new Paragraph({ indent: { left: 540, hanging: 360 }, spacing: { after: 60 }, children: [new TextRun({ text: `${m[1]}.  `, bold: true }), ...runs(m[2])] }))
      continue
    }
    if ((m = line.match(/^\s*[-*] (.*)/))) {
      flush()
      blocks.push(new Paragraph({ indent: { left: 540, hanging: 270 }, spacing: { after: 60 }, children: [new TextRun('•  '), ...runs(m[1])] }))
      continue
    }
    if ((m = line.match(/^> ?(.*)/))) {
      flush()
      blocks.push(new Paragraph({
        spacing: { after: 120 }, indent: { left: 360 },
        border: { left: { style: BorderStyle.SINGLE, size: 12, color: '9CA3AF', space: 8 } },
        children: runs(m[1], { italics: true, color: '4B5563' }),
      }))
      continue
    }
    if (line.trim().startsWith('|')) {
      flush()
      const rows = []
      while (i < lines.length && lines[i].trim().startsWith('|')) {
        if (!/^\|[\s|:-]+\|?$/.test(lines[i].trim())) rows.push(lines[i].trim().replace(/^\||\|$/g, '').split('|').map((c) => c.trim()))
        i++
      }
      i--
      blocks.push(table(rows))
      blocks.push(new Paragraph({ children: [] }))
      continue
    }
    para.push(line.trim())
  }
  flush()
  return { blocks, steps }
}

// ----- the summary page
function summary(role, steps) {
  const shots = steps.flatMap((s) => s.images.map((name) => ({ step: s.title, name })))
  // The sign-in picture belongs to the top of the file, before the first step.
  const signIn = byName.get(`${path.basename(role.file, '.md').replace('executive', 'seller')}-00-sign-in`)
  const rows = [['Step', 'Picture', 'Result']]
  let worked = 0, failed = 0, notRun = 0
  const problems = []
  for (const s of steps) {
    if (!s.images.length) { rows.push([s.title, '—', 'Text only']); continue }
    for (const name of s.images) {
      const r = byName.get(name)
      const exists = fs.existsSync(path.join(WALK, 'images', `${name}.png`))
      let result
      if (r?.ok && exists) { result = 'Worked'; worked++ }
      else if (r && !r.ok) { result = 'FAILED'; failed++; problems.push(`${s.title} (${name}): ${r.error}`) }
      else if (run) { result = exists ? 'Picture is from an earlier run' : 'Not taken'; notRun++; problems.push(`${s.title} (${name}): ${result.toLowerCase()}`) }
      else { result = exists ? 'No run record' : 'Missing'; notRun++ }
      rows.push([s.title, name + '.png', result])
    }
  }
  const verdict = !run ? 'No run record found. Run take-screenshots.mjs first.'
    : failed + notRun === 0 ? 'ALL STEPS WORKED' : `${failed + notRun} STEP(S) NEED ATTENTION`
  const out = [
    new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun('Run summary')] }),
    table([
      ['Role', role.title],
      ['Run started', run ? new Date(run.startedAt).toLocaleString() : '—'],
      ['Application', run?.base ?? '—'],
      ['Pictures expected', String(shots.length)],
      ['Worked', String(worked)],
      ['Failed', String(failed)],
      ['Missing or old', String(notRun)],
      ['Sign-in page', signIn ? (signIn.ok ? 'Worked' : 'FAILED') : '—'],
      ['Verdict', verdict],
    ], [30, 70]),
    new Paragraph({ children: [] }),
  ]
  if (problems.length) {
    out.push(new Paragraph({ children: [new TextRun({ text: 'What needs attention', bold: true, color: 'B91C1C' })] }))
    for (const p of problems) out.push(new Paragraph({ indent: { left: 540, hanging: 270 }, children: [new TextRun('•  '), ...runs(p)] }))
    out.push(new Paragraph({ children: [] }))
  }
  out.push(new Paragraph({ children: [new TextRun({ text: 'Scenario results', bold: true })] }), table(rows, [45, 30, 25]), new Paragraph({ children: [new PageBreak()] }))
  return { blocks: out, verdict }
}

for (const role of ROLES) {
  const md = fs.readFileSync(path.join(WALK, role.file), 'utf8')
  const { blocks, steps } = convert(md)
  const sum = summary(role, steps)
  // The title (first block) goes first, then the summary page, then the scenario.
  const doc = new Document({
    creator: 'BannerAI',
    title: `BannerAI demo: ${role.title}`,
    styles: { default: { document: { run: { font: 'Calibri', size: 22 } } } },
    sections: [{ children: [blocks[0], ...sum.blocks, ...blocks.slice(1)] }],
  })
  const file = path.join(OUT, role.out)
  fs.writeFileSync(file, await Packer.toBuffer(doc))
  console.log(`${role.out}: ${sum.verdict}`)
}
console.log(`\nWord files in ${OUT}`)
