#Requires -Version 7.2
[CmdletBinding()]
param(
    [uri]$BaseUrl = 'http://localhost:8080'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$apiBase = $BaseUrl.AbsoluteUri.TrimEnd('/')
$password = $env:LANGCENTER_DEMO_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    $password = Read-Host 'Demo password from Docker Compose' -MaskInput
}

function Invoke-DemoRequest {
    param([string]$Method, [string]$Path, [string]$Token, [object]$Body)

    $options = @{
        Method = $Method
        Uri = "$apiBase$Path"
        TimeoutSec = 15
    }
    if ($Token) {
        $options.Headers = @{ Authorization = "Bearer $Token" }
    }
    if ($null -ne $Body) {
        $options.ContentType = 'application/json'
        $options.Body = ConvertTo-Json -InputObject $Body -Compress
    }

    Invoke-RestMethod @options
}

function Add-DemoEnrollment {
    param([guid]$ClassId, [hashtable]$Student, [string]$Token)

    $profile = Invoke-DemoRequest -Method Post -Path '/api/students' -Token $Token -Body $Student
    Invoke-DemoRequest -Method Post -Path "/api/classes/$ClassId/enrollments" -Token $Token -Body @{
        studentId = $profile.id
    }
}

$null = Invoke-DemoRequest -Method Get -Path '/healthz'
$registrar = (Invoke-DemoRequest -Method Post -Path '/api/auth/login' -Body @{
    email = 'registrar@langcenter.local'
    password = $password
}).accessToken
$cashier = (Invoke-DemoRequest -Method Post -Path '/api/auth/login' -Body @{
    email = 'cashier@langcenter.local'
    password = $password
}).accessToken

$runId = [guid]::NewGuid().ToString('N').Substring(0, 10)
$trainingClass = Invoke-DemoRequest -Method Post -Path '/api/classes' -Token $registrar -Body @{
    name = "Demo English A1 $runId"
    minimumEnrollment = 2
    tuitionFee = 1000
}
$classId = $trainingClass.id
$students = @(
    @{ fullName = 'Demo Student A'; email = "demo-a-$runId@example.test" }
    @{ fullName = 'Demo Student B'; email = "demo-b-$runId@example.test" }
)
$enrollments = foreach ($student in $students) {
    Add-DemoEnrollment -ClassId $classId -Student $student -Token $registrar
}

$null = Invoke-DemoRequest -Method Post -Path "/api/classes/$classId/launch" -Token $registrar
$partialId = $enrollments[0].id
$eligibleId = $enrollments[1].id
$null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$partialId/payments" -Token $cashier -Body @{
    amount = 400
    requestId = [guid]::NewGuid()
}
$null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$eligibleId/payments" -Token $cashier -Body @{
    amount = 1000
    requestId = [guid]::NewGuid()
}
$null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$eligibleId/exam-admission" -Token $registrar

$underfilledClass = Invoke-DemoRequest -Method Post -Path '/api/classes' -Token $registrar -Body @{
    name = "Demo Tiếng Nhật N5 $runId"
    minimumEnrollment = 2
    tuitionFee = 850000
}
$underfilledEnrollment = Add-DemoEnrollment -ClassId $underfilledClass.id -Token $registrar -Student @{
    fullName = 'Lê Minh An (Demo)'
    email = "demo-n5-$runId@example.test"
}
$waitingStudent = Invoke-DemoRequest -Method Post -Path '/api/students' -Token $registrar -Body @{
    fullName = 'Trần Mai Linh (Demo)'
    email = "demo-waiting-$runId@example.test"
}

$certificateClass = Invoke-DemoRequest -Method Post -Path '/api/classes' -Token $registrar -Body @{
    name = "Demo Tiếng Anh B1 $runId"
    minimumEnrollment = 1
    tuitionFee = 1200000
}
$certificateStudent = @{
    fullName = 'Nguyễn Minh Anh (Demo)'
    email = "demo-b1-$runId@example.test"
}
$certificateEnrollment = Add-DemoEnrollment -ClassId $certificateClass.id -Student $certificateStudent -Token $registrar
$certificateEnrollmentId = $certificateEnrollment.id
$null = Invoke-DemoRequest -Method Post -Path "/api/classes/$($certificateClass.id)/launch" -Token $registrar
$null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$certificateEnrollmentId/payments" -Token $cashier -Body @{
    amount = $certificateEnrollment.tuitionDue
    requestId = [guid]::NewGuid()
}
$null = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$certificateEnrollmentId/exam-admission" -Token $registrar
$certificate = Invoke-DemoRequest -Method Post -Path "/api/enrollments/$certificateEnrollmentId/certificate" -Token $registrar -Body @{
    completionConfirmed = $true
}
$publicVerificationUrl = $certificate.verifyUrl -replace '/api/certificates/verify/([^/?#]+)$', '/certificates/verify/$1'

[pscustomobject]@{
    ClassId = $classId
    PartialEnrollmentId = $partialId
    PartialStudentEmail = $students[0].email
    EligibleEnrollmentId = $eligibleId
    EligibleStudentEmail = $students[1].email
    UnderfilledClassId = $underfilledClass.id
    UnderfilledEnrollmentId = $underfilledEnrollment.id
    WaitingStudentId = $waitingStudent.id
    WaitingStudentEmail = $waitingStudent.email
    CertificateClassId = $certificateClass.id
    CertificateEnrollmentId = $certificateEnrollmentId
    CertificateStudentEmail = $certificateStudent.email
    CertificateId = $certificate.id
    CertificateLookupCode = $certificate.lookupCode
    CertificateVerifyUrl = $publicVerificationUrl
}
