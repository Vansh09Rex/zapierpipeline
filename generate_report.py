import os
from fpdf import FPDF

class PDFReport(FPDF):
    def header(self):
        # Title of project report
        self.set_font('Arial', 'I', 8)
        self.set_text_color(128, 128, 128)
        self.cell(0, 10, 'Shopify-Zapier ASP.NET Zoho CRM Pipeline - Project Integration Report', 0, 0, 'L')
        self.cell(0, 10, 'July 2026', 0, 1, 'R')
        self.line(15, 18, 195, 18)
        self.ln(5)

    def footer(self):
        # Position at 1.5 cm from bottom
        self.set_y(-15)
        self.set_font('Arial', 'I', 8)
        self.set_text_color(128, 128, 128)
        self.line(15, 280, 195, 280)
        # Page number
        self.cell(0, 10, f'Page {self.page_no()}', 0, 0, 'R')

    def chapter_title(self, num, title):
        self.set_font('Arial', 'B', 16)
        self.set_text_color(33, 150, 243) # Sleek blue
        self.cell(0, 10, f'{num}. {title}', 0, 1, 'L')
        self.ln(4)

    def section_title(self, num_str, title):
        self.set_font('Arial', 'B', 12)
        self.set_text_color(48, 63, 159) # Indigo
        self.cell(0, 8, f'{num_str} {title}', 0, 1, 'L')
        self.ln(2)

    def paragraph(self, text):
        self.set_font('Arial', '', 10)
        self.set_text_color(50, 50, 50)
        self.multi_cell(0, 5, text)
        self.ln(3)

    def bullet_point(self, bold_text, desc):
        self.set_font('Arial', 'B', 10)
        self.set_text_color(50, 50, 50)
        self.cell(5, 5, chr(149), 0, 0) # bullet character
        self.cell(40, 5, bold_text, 0, 0)
        self.set_font('Arial', '', 10)
        self.multi_cell(0, 5, desc)
        self.ln(1)

def create_report():
    pdf = PDFReport(orientation='P', unit='mm', format='A4')
    pdf.set_margins(15, 20, 15)
    pdf.set_auto_page_break(auto=True, margin=20)

    # ------------------ PAGE 1: ABBREVIATIONS ------------------
    pdf.add_page()
    pdf.set_font('Arial', 'B', 20)
    pdf.set_text_color(33, 150, 243)
    pdf.cell(0, 15, 'PROJECT INTEGRATION REPORT', 0, 1, 'C')
    pdf.set_font('Arial', 'B', 12)
    pdf.set_text_color(100, 100, 100)
    pdf.cell(0, 5, 'Shopify-Zapier to Zoho CRM Dual-Store ASP.NET Core Pipeline', 0, 1, 'C')
    pdf.ln(15)

    pdf.set_font('Arial', 'B', 16)
    pdf.set_text_color(33, 150, 243)
    pdf.cell(0, 10, 'Abbreviations', 0, 1, 'L')
    pdf.ln(5)

    # Abbreviations Table Header
    pdf.set_font('Arial', 'B', 10)
    pdf.set_fill_color(225, 245, 254) # Light Blue fill
    pdf.cell(40, 8, 'Abbreviation', 1, 0, 'C', 1)
    pdf.cell(140, 8, 'Full Form', 1, 1, 'C', 1)

    # Table rows
    abbreviations = [
        ('API', 'Application Programming Interface'),
        ('CRM', 'Customer Relationship Management'),
        ('JWT', 'JSON Web Token'),
        ('HMAC', 'Hash-based Message Authentication Code'),
        ('SHA', 'Secure Hash Algorithm'),
        ('EF Core', 'Entity Framework Core'),
        ('SQL', 'Structured Query Language'),
        ('JSON', 'JavaScript Object Notation'),
        ('URL', 'Uniform Resource Locator'),
        ('TLS', 'Transport Layer Security'),
        ('HTTPS', 'HyperText Transfer Protocol Secure'),
        ('XML', 'eXtensible Markup Language'),
        ('WSL', 'Windows Subsystem for Linux'),
        ('DDL', 'Data Definition Language'),
        ('REST', 'Representational State Transfer')
    ]

    pdf.set_font('Arial', '', 9)
    for abv, full in abbreviations:
        pdf.cell(40, 7, f' {abv}', 1, 0, 'L')
        pdf.cell(140, 7, f' {full}', 1, 1, 'L')
    
    # ------------------ PAGE 2: INTRODUCTION ------------------
    pdf.add_page()
    pdf.chapter_title('1', 'Introduction')
    pdf.paragraph(
        'Integrating diverse e-commerce networks with internal enterprise customer relationship platforms '
        'demands secure, highly-performant, and traceable pipelines. In modern retail logistics, transaction '
        'information generated at the client end must be verified for authenticity and securely written into both '
        'structured analytical data repositories and operational logs before being synchronized with outbound business '
        'management suites like Zoho CRM.'
    )
    pdf.paragraph(
        'In response to this architectural need, this project presents a "Dual Mode Storage Integration Pipeline" '
        'built on ASP.NET Core 10.0. The pipeline securely receives order data via ingress endpoints adapted for both '
        'Shopify webhook channels and Zapier customized CLI applications, processes the data through dual relational '
        '(PostgreSQL) and non-relational (MongoDB) databases, and synchronizes the validated information with Zoho CRM.'
    )
    
    pdf.section_title('1.1', 'Motivation')
    pdf.paragraph(
        'E-commerce systems deal with high-frequency transactions that require immediate processing, audit trails, and '
        'synchronization. Standard setups suffer from limitations: systems optimized for fast webhook response often '
        'fail to maintain audit logs of raw payloads, leading to tracing failures when outbound integrations drop. '
        'Moreover, lacking robust ingress key signatures makes endpoints highly vulnerable to spoofed transactions.'
    )
    pdf.paragraph('Our core project objectives include:')
    pdf.bullet_point('E-Commerce Security:', 'Implementing SHA256 HMAC handshakes for Shopify webhooks and OAuth 2.0 JWT for Zapier integrations.')
    pdf.bullet_point('Auditability:', 'Creating a dual-store logging model (PostgreSQL for clean order structures, MongoDB for raw payload audits).')
    pdf.bullet_point('Interoperability:', 'Structuring a Zoho CRM client service with mock failover profiles to test endpoints end-to-end.')

    # ------------------ PAGE 3: LITERATURE SURVEY ------------------
    pdf.add_page()
    pdf.chapter_title('2', 'Literature Survey')
    pdf.paragraph(
        'Modern microservice design patterns heavily rely on message queues and API gateway patterns '
        'to establish reliable distributed pipelines (Richardson, 2018). In e-commerce, the decoupling of '
        'transaction collection from CRM synchronization is crucial to avoid bottlenecking checkout operations. '
        'Standard models highlight the use of asynchronous event processing (Newman, 2019).'
    )
    pdf.paragraph(
        'Security frameworks for webhooks, specifically regarding cryptographic HMAC verification, are established '
        'standards for validating request headers (RFC 2104). The use of JWT Bearer tokens for third-party application '
        'authorization provides secure stateless client authentication without needing database locks for active sessions (RFC 7519).'
    )
    pdf.paragraph(
        'On the database front, polyglot persistence (using SQL and NoSQL databases in tandem) is recognized as an '
        'optimal strategy (Sadalage & Fowler, 2012). Storing transactional records in relational systems ensures transactional '
        'safety, while storing original, unstructured JSON payloads in document databases preserves strict logs without '
        'forcing schema constraints on incoming payloads.'
    )

    # ------------------ PAGE 4: PROBLEM STATEMENT ------------------
    pdf.add_page()
    pdf.chapter_title('3', 'Problem Statement and Objectives')
    pdf.section_title('3.1', 'Problem Statement')
    pdf.paragraph(
        'In modern enterprise architectures, processing transactions across multiple external boundaries '
        '(like Shopify webhooks and Zapier customized platforms) introduces severe challenges regarding:'
    )
    pdf.bullet_point('Ingress Security:', 'Protecting API endpoints from spoofed request payloads or injection threats.')
    pdf.bullet_point('System Reliability:', 'Syncing transactions to external CRMs (like Zoho CRM) without risking data loss when APIs go offline.')
    pdf.bullet_point('Observability:', 'Ensuring all request contents and original JSON strings are preserved for audit checks.')
    
    pdf.section_title('3.2', 'Project Objectives')
    pdf.paragraph(
        'The proposed architecture resolves these issues by implementing a secure, dual-store ASP.NET Core '
        'web api pipeline. Our specific goals are:'
    )
    pdf.bullet_point('Security Ingress:', 'Provide constant-time HMAC validation and stateful JWT token issuance.')
    pdf.bullet_point('Data Persistence:', 'Use PostgreSQL for query-optimized order records and MongoDB for unstructured payload logs.')
    pdf.bullet_point('Outbound Sync:', 'Synchronize clean data records with Zoho CRM via OAuth2 token exchange with mock fallbacks.')

    # ------------------ PAGE 5: REQUIREMENTS ------------------
    pdf.add_page()
    pdf.chapter_title('4', 'Requirements')
    pdf.section_title('4.1', 'Functional Requirements')
    pdf.bullet_point('FR01 Token Auth:', 'Zapier client obtains JWT token using client credentials and sends it in Bearer scheme.')
    pdf.bullet_point('FR02 Webhook Security:', 'Shopify webhook endpoint computes and validates HMAC signature against configurations.')
    pdf.bullet_point('FR03 Polyglot Storage:', 'Write structured order database schema to PostgreSQL and raw json requests to MongoDB.')
    pdf.bullet_point('FR04 Zoho CRM Sync:', 'Format clean order objects and POST them to Zoho CRM Contacts endpoint.')
    pdf.bullet_point('FR05 Sandbox Testing:', 'Toggle mock configurations for Zoho CRM and databases to validate flows offline.')

    pdf.section_title('4.2', 'Non-Functional Requirements')
    pdf.bullet_point('NFR01 Performance:', 'Ingress API requests complete in < 500ms, and database writes execute asynchronously.')
    pdf.bullet_point('NFR02 Security:', 'Encrypt data-in-transit via HTTPS and implement secure, constant-time bytes comparisons.')
    pdf.bullet_point('NFR03 Maintainability:', 'Provide a containerized environment using multi-stage Dockerfiles and docker-compose.yml.')

    # ------------------ PAGE 6: METHODOLOGY ------------------
    pdf.add_page()
    pdf.chapter_title('5', 'Methodology')
    pdf.section_title('5.1', 'Development Approach: Agile Methodology')
    pdf.paragraph(
        'The project adopted the Agile software development lifecycle, utilizing short iteration cycles. '
        'Each iteration focused on specific components of the pipeline (API layer, Database configuration, Outbound clients, and Containerization).'
    )
    
    pdf.section_title('5.2', 'System Architecture Overview')
    pdf.paragraph(
        'The system is divided into three coordinated architectural layers:'
    )
    pdf.bullet_point('1. API & Security:', 'Kestrel-hosted endpoints verifying Shopify webhook HMAC signatures and Zapier OAuth 2.0 JWT Bearer headers.')
    pdf.bullet_point('2. Data Storage:', 'Asynchronous polyglot persistence writing structured data using EF Core and raw audits using MongoDB C# Driver.')
    pdf.bullet_point('3. Outbound CRM Sync:', 'Zoho CRM HTTP client service with automatic token refresh flows and mock sandbox controls.')

    # ------------------ PAGE 7: SYSTEM DESIGN & SCHEMA ------------------
    pdf.add_page()
    pdf.chapter_title('6', 'System Design and Schema')
    pdf.paragraph(
        'The backend is engineered around clean architectural patterns, isolating the controller routes from database repositories '
        'and external integration clients. The database schemas emphasize strict data contracts for orders, combined with loose document stores '
        'for request payloads.'
    )
    pdf.section_title('6.1', 'Polyglot Storage Design')
    pdf.paragraph(
        'By storing structured values in PostgreSQL, we optimize queries for order status, sync status, and total sales volume. '
        'Concurrently, MongoDB logs all headers and the exact incoming JSON string. This prevents database lockups if the order schema changes '
        'on the client side, ensuring audit compliance.'
    )

    # ------------------ PAGE 8: DATAFLOW ------------------
    pdf.add_page()
    pdf.chapter_title('7', 'Data Flow')
    pdf.paragraph(
        'The system exhibits two main execution pathways depending on the transaction origin:'
    )
    pdf.section_title('7.1', 'Shopify Webhook Dataflow')
    pdf.bullet_point('A. Webhook POST:', 'Shopify dispatches order payload to /api/webhooks/shopify containing the HMAC signature header.')
    pdf.bullet_point('B. Verification:', 'Controller checks signature against configurations. Mismatched calls return 401 Unauthorized.')
    pdf.bullet_point('C. Logging & Storage:', 'Raw body logged to MongoDB; structured entity saved to PostgreSQL Orders table.')
    pdf.bullet_point('D. Outbound Sync:', 'ZohoCrmService dispatches order fields to Zoho. SyncStatus is updated to "Synced" or "Failed" in Postgres.')

    pdf.section_title('7.2', 'Zapier Dataflow')
    pdf.bullet_point('A. Token Issue:', 'Zapier requests JWT token from /api/auth/token using developer client credentials.')
    pdf.bullet_point('B. Authenticated Request:', 'Zapier calls /api/orders sending the JWT in Authorization: Bearer header.')
    pdf.bullet_point('C. Execution:', 'Controller authorizes request, logs raw payload to Mongo, writes to PostgreSQL, and triggers Zoho sync.')

    # ------------------ PAGE 9: DATABASE SCHEMA ------------------
    pdf.add_page()
    pdf.chapter_title('8', 'Database and Table Schema')
    pdf.section_title('8.1', 'PostgreSQL Orders Schema (DDL)')
    pdf.paragraph(
        'The structured relational table "orders" stores the records generated by the pipeline. '
        'An explicit DDL script compiles the database schema:'
    )
    
    # Draw simple schema representation box
    pdf.set_font('Courier', 'B', 8)
    pdf.set_fill_color(240, 240, 240)
    schema_code = (
        "CREATE TABLE orders (\n"
        "    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),\n"
        "    order_id VARCHAR(100) NOT NULL UNIQUE,\n"
        "    customer_email VARCHAR(255) NOT NULL,\n"
        "    total_amount NUMERIC(12, 2) NOT NULL,\n"
        "    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,\n"
        "    sync_status VARCHAR(50) NOT NULL DEFAULT 'Pending',\n"
        "    zoho_record_id VARCHAR(100),\n"
        "    source VARCHAR(50) NOT NULL\n"
        ");"
    )
    pdf.multi_cell(0, 4, schema_code, 1, 'L', 1)
    pdf.ln(5)

    pdf.section_title('8.2', 'MongoDB Raw Audit Log Model')
    pdf.paragraph(
        'The document model "RawPayloadLog" stores the original JSON string and request metadata:'
    )
    pdf.bullet_point('Id:', 'MongoDB BsonObjectId representing the document key.')
    pdf.bullet_point('PayloadType:', 'Identifies client source (ShopifyWebhook or ZapierRequest).')
    pdf.bullet_point('RawJson:', 'The exact unmodified UTF-8 HTTP request body string.')
    pdf.bullet_point('Headers:', 'Dictionary of all request headers, preserving signature data.')

    # ------------------ PAGE 10: IMPLEMENTATION ------------------
    pdf.add_page()
    pdf.chapter_title('9', 'Implementation')
    pdf.paragraph(
        'The pipeline is written in C# targeting .NET 10.0. Security, validation, database repository access, '
        'and Zoho CRM clients coordinate seamlessly using standard dependency injection.'
    )
    pdf.section_title('9.1', 'Shopify HMAC Verification Logic')
    pdf.paragraph(
        'The verification calculates the SHA256 HMAC of the request body bytes using the secret key, '
        'converts it to a Base64 string, and compares it to the header using constant-time comparison:'
    )
    pdf.set_font('Courier', 'B', 8)
    pdf.set_fill_color(240, 240, 240)
    hmac_code = (
        "var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));\n"
        "var computed = Convert.ToBase64String(hmac.ComputeHash(bodyBytes));\n"
        "return CryptographicOperations.FixedTimeEquals(\n"
        "    Encoding.UTF8.GetBytes(computed),\n"
        "    Encoding.UTF8.GetBytes(hmacHeader));"
    )
    pdf.multi_cell(0, 4, hmac_code, 1, 'L', 1)
    pdf.ln(5)

    pdf.section_title('9.2', 'Zoho CRM Outbound Service')
    pdf.paragraph(
        'ZohoCrmService fetches short-lived OAuth access tokens using the refresh token flow, caching them '
        'until expiration. In mock mode (UseMock = true), the service bypasses external API requests, logging '
        'formatted payloads to the console and returning simulated record IDs (e.g., ZOHO_MOCK_...) to verify the pipeline.'
    )

    # ------------------ PAGE 11: DEPLOYMENT CONTEXT ------------------
    pdf.add_page()
    pdf.chapter_title('10', 'Deployment Context and Infrastructure')
    pdf.paragraph(
        'The pipeline uses Docker containerization to bundle database dependencies, facilitating one-click deployments. '
        'The deployment is defined using two primary assets:'
    )
    pdf.section_title('10.1', 'Dockerfile (ASP.NET Core Web API)')
    pdf.paragraph(
        'Multi-stage build compiling project in a dotnet 10.0 SDK container and executing inside '
        'a lightweight aspnet 10.0 runtime container.'
    )
    pdf.section_title('10.2', 'Docker Compose Configuration')
    pdf.paragraph(
        'Manages network and volumes for PostgreSQL (16) and MongoDB (7) images, ensuring healthchecks are met before '
        'launching the backend container. Ports are mapped to: Web API (5080), Postgres (5432), and MongoDB (27017).'
    )

    # ------------------ PAGE 12: TESTING & VALIDATION ------------------
    pdf.add_page()
    pdf.chapter_title('11', 'Testing and Validation')
    pdf.paragraph(
        'To verify the architecture, we ran automated test cases covering security, validation, database integration, '
        'and CRM updates under mock databases.'
    )
    
    # Test cases table
    pdf.set_font('Arial', 'B', 8)
    pdf.set_fill_color(225, 245, 254)
    pdf.cell(15, 6, 'ID', 1, 0, 'C', 1)
    pdf.cell(40, 6, 'Scenario', 1, 0, 'C', 1)
    pdf.cell(35, 6, 'Input Data', 1, 0, 'C', 1)
    pdf.cell(60, 6, 'Expected Result', 1, 0, 'C', 1)
    pdf.cell(30, 6, 'Status', 1, 1, 'C', 1)

    test_cases = [
        ('TC001', 'Valid Webhook', 'Valid HMAC + JSON', 'Record accepted, Zoho sync success', 'Passed'),
        ('TC002', 'Invalid HMAC', 'Bad HMAC header', '401 Unauthorized, rejected', 'Passed'),
        ('TC003', 'Missing Fields', 'JSON missing OrderId', '400 Bad Request, validation fail', 'Passed'),
        ('TC004', 'Valid Zapier JWT', 'Bearer JWT header', 'Auth success, order processed', 'Passed'),
        ('TC005', 'Duplicate Order', 'SHOP-777 sent twice', 'Database filters duplicate, OK returned', 'Passed')
    ]

    pdf.set_font('Arial', '', 8)
    for tid, scen, inp, exp, stat in test_cases:
        pdf.cell(15, 6, f' {tid}', 1, 0, 'C')
        pdf.cell(40, 6, f' {scen}', 1, 0, 'L')
        pdf.cell(35, 6, f' {inp}', 1, 0, 'L')
        pdf.cell(60, 6, f' {exp}', 1, 0, 'L')
        pdf.cell(30, 6, f' {stat}', 1, 1, 'C')
    pdf.ln(5)

    pdf.section_title('11.1', 'HMAC verification log proof')
    pdf.paragraph(
        'Running test-shopify.ps1 prints: "Shopify webhook signature verified successfully" and completes '
        'the relational write, showing full ingress security compliance.'
    )

    # ------------------ PAGE 13: CONCLUSION & CHALLENGES ------------------
    pdf.add_page()
    pdf.chapter_title('12', 'Conclusion')
    pdf.paragraph(
        'The project successfully implements a containerized, polyglot persistence pipeline integrating Shopify and '
        'Zapier transaction streams with Zoho CRM. All security requirements, including SHA256 HMAC verification '
        'and JWT token gating, have been verified. The application is ready for production staging deployment.'
    )

    pdf.chapter_title('13', 'Challenges and Lessons Learned')
    pdf.section_title('13.1', 'Polyglot Transaction Synchronization')
    pdf.paragraph(
        'Synchronizing data across relational, document, and CRM APIs simultaneously introduces network latency risk. '
        'Executing DB writes asynchronously and implementing mock settings enabled testing without live dependency bottlenecks.'
    )
    pdf.section_title('13.2', 'Shopify HMAC request stream reading')
    pdf.paragraph(
        'ASP.NET Core consumes request bodies during model binding, blocking signature checks. Enabling Request Buffering '
        'resolved this, allowing signature calculations and model binding to read the body stream consecutively.'
    )

    # ------------------ PAGE 14: FUTURE SCOPE & REFERENCES ------------------
    pdf.add_page()
    pdf.chapter_title('14', 'Future Scope')
    pdf.paragraph(
        '1. Integration with a live Zoho CRM instance using production client credentials.\n'
        '2. Implementing a Redis cache layer for JWT tokens to support horizontal API scaling.\n'
        '3. Exposing a frontend dashboard using ReactJS to monitor order volumes and integration success rates in real time.'
    )

    pdf.chapter_title('15', 'References')
    pdf.set_font('Arial', '', 8)
    pdf.multi_cell(0, 4, 
        '[1] Richardson, C. (2018). Microservices Patterns: With examples in Java. Manning Publications.\n'
        '[2] Newman, S. (2019). Monolith to Microservices: Evolutionary Patterns to Transform Your Monolith. O\'Reilly Media.\n'
        '[3] Sadalage, P. J., & Fowler, M. (2012). NoSQL Distilled: A Brief Guide to the Emerging World of Polyglot Persistence. Addison-Wesley.\n'
        '[4] RFC 7519: JSON Web Token (JWT). Internet Engineering Task Force (IETF).\n'
        '[5] RFC 2104: HMAC: Keyed-Hashing for Message Authentication. Internet Engineering Task Force (IETF).\n'
        '[6] Entity Framework Core Core Concepts. Microsoft Documentation.'
    )
    
    # Save PDF
    output_filename = "C:\\Users\\vansh\\.gemini\\antigravity\\scratch\\TeamZapier\\project_integration_report.pdf"
    pdf.output(output_filename, 'F')
    print(f"PDF Report generated successfully: {output_filename}")

if __name__ == '__main__':
    create_report()
