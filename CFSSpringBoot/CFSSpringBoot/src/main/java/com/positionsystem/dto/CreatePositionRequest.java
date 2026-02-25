
package com.positionsystem.dto;

import lombok.Data;
import java.math.BigDecimal;
import java.time.LocalDate;

@Data
public class CreatePositionRequest {
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
    private Boolean isFeExchange;
}
