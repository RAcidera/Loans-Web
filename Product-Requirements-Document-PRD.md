# Product Requirements Document (PRD)

## Product name
**Vetra AI**

## Version
2.0 – Northern Luzon Edition

## Product vision

Vetra AI is an AI-powered veterinary and livestock healthcare platform designed specifically for **Northern Luzon**, starting with **Ilocos Norte**. The platform connects farmers, pet owners, veterinarians, and municipal veterinary offices through AI-assisted animal health screening, veterinary booking, vaccination management, and digital medical records.

The long-term vision is to become the **digital veterinary infrastructure of Northern Luzon**, improving animal health outcomes, farm productivity, and access to veterinary services.

---

# Product strategy

## Geographic focus

### Phase 1 (Pilot Province)

- Ilocos Norte

### Phase 2

- Ilocos Sur
- La Union
- Pangasinan

### Phase 3

- Abra
- Cagayan
- Isabela
- Apayao

---

# Primary users

## Livestock owners (Primary Segment)

- Backyard pig raisers
- Commercial pig farms
- Poultry farmers
- Cattle raisers
- Goat raisers

## Pet owners (Secondary Segment)

- Dog owners
- Cat owners
- Companion animal owners

## Veterinary professionals

- Private veterinarians
- Mobile veterinarians
- Farm veterinarians
- Veterinary technicians

## Government partners

- Municipal Veterinary Offices
- Provincial Veterinary Offices
- DA-affiliated veterinary programs

---

# Business objectives

## Primary objectives

- Improve access to veterinary services in Northern Luzon.
- Reduce livestock mortality through early disease screening.
- Increase vaccination compliance.
- Digitize farm and animal medical records.
- Create a sustainable veterinary service marketplace.

## Success metrics

- Registered farmers
- Registered veterinarians
- Monthly consultations
- Vaccination compliance rate
- AI screening usage
- Monthly recurring revenue
- Commission revenue
- Veterinarian retention

---

# Problem statement

Farmers and pet owners in Northern Luzon frequently experience:

- Limited veterinarian availability
- Long travel distances
- Delayed disease detection
- Missed vaccinations
- Poor record keeping
- Lack of treatment history
- Limited access to veterinary specialists

Vetra AI provides a centralized digital platform for animal healthcare and veterinary access.

---

# Product scope

## MVP (6 months)

### User registration

The system shall support:

- Farmer accounts
- Pet owner accounts
- Veterinarian accounts
- Veterinary technician accounts

### Animal profiles

For each animal:

- Species
- Breed
- Age
- Sex
- Weight
- Photo
- Farm location
- Vaccination history
- Treatment history
- Pregnancy status
- Identification number

### AI symptom checker

Users can enter:

- Loss of appetite
- Fever
- Cough
- Diarrhea
- Vomiting
- Lameness
- Skin lesions
- Swelling
- Respiratory symptoms
- Reproductive issues

AI output:

- Possible disease category
- Urgency level
- Isolation recommendation
- Biosecurity advice
- Veterinary consultation recommendation

### AI photo analysis

Upload images of:

- Skin infections
- Wounds
- Eyes
- Feet
- Feces
- Swelling
- Parasites

AI provides visual assessment and risk classification.

### Veterinary booking

Book:

- Farm visits
- Home visits
- Clinic consultations
- Vaccination services
- Injection services
- Emergency consultations

### Digital medical records

Store:

- Diagnoses
- Treatments
- Medications
- Vaccinations
- Deworming
- Laboratory results
- Veterinary notes

### Vaccination management

Track:

- Vaccination schedules
- Booster reminders
- Vaccination certificates
- Compliance status

### Notifications

Push notifications and SMS:

- Vaccination due dates
- Appointment reminders
- Medication reminders
- Disease alerts

---

# Disease coverage

## Swine

- African Swine Fever (ASF)
- Classical Swine Fever
- PRRS
- Pneumonia
- Swine dysentery
- Parasitic infections

## Poultry

- Newcastle disease
- Coccidiosis
- Infectious bronchitis
- Avian influenza alerts

## Cattle

- Mastitis
- Pneumonia
- Foot rot
- Internal parasites

## Goats

- Pneumonia
- Parasitic infections
- Nutritional disorders

## Pets

- Skin disease
- Ear infections
- Gastrointestinal illness
- Respiratory disease
- Vaccination assessment

---

# Functional requirements

## Authentication

The system shall:

- Register users
- Verify phone numbers
- Support OTP login
- Support biometric login
- Support Google login

## Veterinarian verification

The system shall:

- Verify PRC license information
- Approve veterinarian accounts
- Display verified veterinarian badges

## Animal management

Users shall:

- Add multiple animals
- Upload photos
- Update weight
- Archive deceased animals
- Transfer ownership

## AI engine

The AI module shall:

- Accept text symptoms
- Accept image uploads
- Generate triage recommendations
- Flag emergency cases
- Recommend veterinarian consultation

The AI shall **not issue definitive diagnoses**.

## Appointment management

The system shall:

- Display veterinarian availability
- Book appointments
- Reschedule appointments
- Cancel appointments
- Send reminders
- Track appointment status

## Medical records

Veterinarians shall:

- Create consultation records
- Upload laboratory reports
- Record vaccinations
- Prescribe medications
- Attach clinical photos

Users shall:

- View records
- Download records
- Share records with veterinarians

---

# Flutter application requirements

## Platforms

- Android (Priority)
- iOS

## Design system

Material 3

Primary color: Emerald green

Responsive mobile-first design

Offline-capable local storage

Smooth animations

Dark mode support

## Navigation

Bottom navigation:

- Home
- Animals
- AI
- Appointments
- Profile

Floating AI assistant button

---

# Payment and monetization

## Business model

Hybrid marketplace plus subscription.

### Commission-based services

| Service | Platform commission |
|--------|----------------------|
| Farm visit | 12% |
| Home pet visit | 15% |
| Clinic consultation | 15% |
| Vaccination service | 10% |
| Injection service | 10% |
| Emergency consultation | 18% |

### Farmer subscription

## Free

- Up to 20 animals
- Basic AI symptom checker
- Vaccination reminders
- Basic records

## Farm Pro – ₱299/month

- Unlimited animals
- AI photo analysis
- Farm analytics
- Pregnancy tracking
- Weight monitoring
- Mortality reports
- Priority veterinary booking

### Enterprise Farm – ₱1,999/month

- Multi-user access
- Multiple farms
- Advanced analytics
- Export reports
- Staff management

---

# Payment requirements

Integrate:

- GCash
- Maya
- Bank transfer
- QR Ph
- Cash on visit

The system shall:

- Process appointment payments
- Calculate commissions
- Generate veterinarian payouts
- Generate invoices
- Track payment history

---

# Municipal veterinary integration

The system shall support:

- Municipal veterinarian directory
- Vaccination campaign announcements
- Disease outbreak alerts
- Government advisories
- Referral coordination

---

# Non-functional requirements

## Performance

- Response time under 2 seconds
- AI screening under 10 seconds

## Availability

- 99.5% uptime

## Security

- Encrypted medical records
- Secure authentication
- Role-based access
- Audit logs

## Scalability

Support expansion across Northern Luzon provinces.

---

# MVP roadmap

## Month 1

- Requirements
- Flutter UI/UX
- Architecture

## Month 2

- Authentication
- Animal profiles

## Month 3

- AI symptom checker
- AI photo upload

## Month 4

- Appointment booking
- Veterinarian dashboard

## Month 5

- Medical records
- Vaccination module

## Month 6

- Payments
- Notifications
- Pilot launch in Ilocos Norte

---

# Key performance indicators

### First 12 months

- 3,000 registered farmers
- 500 pet owners
- 75 verified veterinarians
- 8,000 registered animals
- 3,000 completed consultations
- 70% vaccination reminder engagement
- ₱250,000 monthly platform revenue

---

# Long-term vision

Vetra AI will become the leading AI-powered veterinary and livestock platform in Northern Luzon by improving animal health, supporting local veterinarians, reducing farm losses, and digitizing regional animal healthcare services through accessible mobile technology.