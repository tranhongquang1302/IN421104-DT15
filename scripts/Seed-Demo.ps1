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
    $profile = Invoke-DemoRequest -Method Post -Path '/api/students' -Token $registrar -Body $student
    Invoke-DemoRequest -Method Post -Path "/api/classes/$classId/enrollments" -Token $registrar -Body @{
        studentId = $profile.id
    }
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

[pscustomobject]@{
    ClassId = $classId
    PartialEnrollmentId = $partialId
    PartialStudentEmail = $students[0].email
    EligibleEnrollmentId = $eligibleId
    EligibleStudentEmail = $students[1].email
}
