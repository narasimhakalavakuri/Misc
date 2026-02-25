
package com.positionsystem.service;

import com.positionsystem.dto.CreatePositionRequest;
import com.positionsystem.dto.PositionEntryDto;
import com.positionsystem.entity.EntryStatus;
import com.positionsystem.entity.PositionEntry;
import com.positionsystem.repository.PositionEntryRepository;
import com.positionsystem.exception.ResourceNotFoundException;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Transactional
public class PositionEntryService {

    private final PositionEntryRepository positionEntryRepository;

    public PositionEntryDto createPositionEntry(CreatePositionRequest request, String createdBy) {
        PositionEntry entry = new PositionEntry();
        
        // Generate UID
        entry.setUid(generateUid());
        entry.setEntryDate(request.getEntryDate());
        entry.setValueDate(request.getValueDate());
        entry.setDepartment(request.getDepartment());
        entry.setTransactionType(request.getTransactionType());
        entry.setReference(request.getReference());
        entry.setTheirReference(request.getTheirReference());
        entry.setInwardCurrency(request.getInwardCurrency());
        entry.setInwardAmount(request.getInwardAmount());
        entry.setOutwardCurrency(request.getOutwardCurrency());
        entry.setOutwardAmount(request.getOutwardAmount());
        entry.setExchangeRate(request.getExchangeRate());
        entry.setCalcOperator(request.getCalcOperator());
        entry.setInwardAccount(request.getInwardAccount());
        entry.setInwardAccountName(request.getInwardAccountName());
        entry.setOutwardAccount(request.getOutwardAccount());
        entry.setOutwardAccountName(request.getOutwardAccountName());
        entry.setStatus(EntryStatus.INCOMPLETE);
        entry.setCreatedBy(createdBy);
        entry.setCreatedDate(LocalDateTime.now());
        entry.setIsFeExchange(request.getIsFeExchange() != null ? request.getIsFeExchange() : false);

        PositionEntry saved = positionEntryRepository.save(entry);
        return mapToDto(saved);
    }

    public List<PositionEntryDto> getPendingApprovals(String department, LocalDate date) {
        return positionEntryRepository.findAll().stream()
            .filter(e -> e.getStatus() == EntryStatus.PENDING)
            .filter(e -> e.getDepartment().equals(department))
            .filter(e -> e.getValueDate().equals(date))
            .map(this::mapToDto)
            .collect(Collectors.toList());
    }

    public List<PositionEntryDto> getIncompleteEntries(String department) {
        return positionEntryRepository.findAll().stream()
            .filter(e -> e.getStatus() == EntryStatus.INCOMPLETE)
            .filter(e -> e.getDepartment().equals(department))
            .map(this::mapToDto)
            .collect(Collectors.toList());
    }

    public List<PositionEntryDto> getCorrectionEntries(String department) {
        return positionEntryRepository.findAll().stream()
            .filter(e -> e.getStatus() == EntryStatus.CORRECTION)
            .filter(e -> e.getDepartment().equals(department))
            .map(this::mapToDto)
            .collect(Collectors.toList());
    }

    public void checkoutEntry(String uid, String username) {
        PositionEntry entry = positionEntryRepository.findByUid(uid)
            .orElseThrow(() -> new ResourceNotFoundException("Entry not found"));
        
        if (entry.getCheckedOutBy() != null && !entry.getCheckedOutBy().isEmpty()) {
            throw new RuntimeException("Entry already checked out by: " + entry.getCheckedOutBy());
        }
        
        entry.setCheckedOutBy(username);
        entry.setModifiedDate(LocalDateTime.now());
        positionEntryRepository.save(entry);
    }

    public void checkinEntry(String uid, String username) {
        PositionEntry entry = positionEntryRepository.findByUid(uid)
            .orElseThrow(() -> new ResourceNotFoundException("Entry not found"));
        
        if (!username.equals(entry.getCheckedOutBy())) {
            throw new RuntimeException("Entry not checked out by you");
        }
        
        entry.setCheckedOutBy(null);
        entry.setModifiedDate(LocalDateTime.now());
        positionEntryRepository.save(entry);
    }

    public void approveEntry(String uid, String approvedBy) {
        PositionEntry entry = positionEntryRepository.findByUid(uid)
            .orElseThrow(() -> new ResourceNotFoundException("Entry not found"));
        
        entry.setStatus(EntryStatus.APPROVED);
        entry.setApprovedDate(LocalDateTime.now());
        entry.setModifiedBy(approvedBy);
        entry.setModifiedDate(LocalDateTime.now());
        entry.setCheckedOutBy(null);
        
        positionEntryRepository.save(entry);
    }

    public void rejectEntry(String uid, String reason, String rejectedBy) {
        PositionEntry entry = positionEntryRepository.findByUid(uid)
            .orElseThrow(() -> new ResourceNotFoundException("Entry not found"));
        
        entry.setStatus(EntryStatus.REJECTED);
        entry.setModifiedBy(rejectedBy);
        entry.setModifiedDate(LocalDateTime.now());
        entry.setCheckedOutBy(null);
        
        positionEntryRepository.save(entry);
    }

    public void deleteEntry(String uid) {
        PositionEntry entry = positionEntryRepository.findByUid(uid)
            .orElseThrow(() -> new ResourceNotFoundException("Entry not found"));
        
        positionEntryRepository.delete(entry);
    }

    public PositionEntryDto getByUid(String uid) {
        PositionEntry entry = positionEntryRepository.findByUid(uid)
            .orElseThrow(() -> new ResourceNotFoundException("Entry not found"));
        return mapToDto(entry);
    }

    private String generateUid() {
        return LocalDateTime.now().toString().replaceAll("[^0-9]", "") + 
               UUID.randomUUID().toString().substring(0, 5);
    }

    private PositionEntryDto mapToDto(PositionEntry entry) {
        PositionEntryDto dto = new PositionEntryDto();
        dto.setId(entry.getId());
        dto.setUid(entry.getUid());
        dto.setEntryDate(entry.getEntryDate());
        dto.setValueDate(entry.getValueDate());
        dto.setDepartment(entry.getDepartment());
        dto.setTransactionType(entry.getTransactionType());
        dto.setReference(entry.getReference());
        dto.setTheirReference(entry.getTheirReference());
        dto.setInwardCurrency(entry.getInwardCurrency());
        dto.setInwardAmount(entry.getInwardAmount());
        dto.setOutwardCurrency(entry.getOutwardCurrency());
        dto.setOutwardAmount(entry.getOutwardAmount());
        dto.setExchangeRate(entry.getExchangeRate());
        dto.setCalcOperator(entry.getCalcOperator());
        dto.setInwardAccount(entry.getInwardAccount());
        dto.setInwardAccountName(entry.getInwardAccountName());
        dto.setOutwardAccount(entry.getOutwardAccount());
        dto.setOutwardAccountName(entry.getOutwardAccountName());
        dto.setStatus(entry.getStatus());
        dto.setCheckedOutBy(entry.getCheckedOutBy());
        dto.setApprovedDate(entry.getApprovedDate());
        dto.setCreatedBy(entry.getCreatedBy());
        dto.setCreatedDate(entry.getCreatedDate());
        dto.setModifiedBy(entry.getModifiedBy());
        dto.setModifiedDate(entry.getModifiedDate());
        dto.setIsFeExchange(entry.getIsFeExchange());
        return dto;
    }
}
