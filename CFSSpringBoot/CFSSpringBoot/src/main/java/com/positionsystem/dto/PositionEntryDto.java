
package com.positionsystem.dto;

import com.positionsystem.entity.EntryStatus;
import lombok.Data;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;

@Data
public class PositionEntryDto {
    private Long id;
    private String uid;
    private LocalDate entryDate;
    private LocalDate valueDate;
    private String department;
    private String transactionType;
    private String reference;
    private String theirReference;
    private String inwardCurrency;
    private BigDecimal inwardAmount;
    private String outwardCurrency;
    private BigDecimal outwardAmount;
    private BigDecimal exchangeRate;
    private String calcOperator;
    private String inwardAccount;
    private String inwardAccountName;
    private String outwardAccount;
    private String outwardAccountName;
    private EntryStatus status;
    private String checkedOutBy;
    private LocalDateTime approvedDate;
    private String createdBy;
    private LocalDateTime createdDate;
    private String modifiedBy;
    private LocalDateTime modifiedDate;
    private Boolean isFeExchange;
}
